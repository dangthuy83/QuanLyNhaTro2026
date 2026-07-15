using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MySqlConnector;

return await MigrationRunner.RunAsync(args);

internal static class MigrationRunner
{
    private const string ConnectionVariable = "QUANLYNHATRO_CONNECTION";

    public static async Task<int> RunAsync(string[] args)
    {
        try
        {
            var command = args.FirstOrDefault()?.ToLowerInvariant() ?? "status";
            if (command is not ("status" or "bootstrap" or "apply-next"))
                throw new InvalidOperationException("Usage: status | bootstrap | apply-next");

            var root = FindRepositoryRoot();
            var manifest = LoadManifest(root);
            ValidateManifestFiles(root, manifest);
            var connectionString = Environment.GetEnvironmentVariable(ConnectionVariable);
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException($"Set {ConnectionVariable}; the runner never reads appsettings.json.");

            await using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            if (command == "bootstrap") await BootstrapAsync(connection, manifest);
            else if (command == "apply-next") await ApplyNextAsync(connection, root, manifest);
            await PrintStatusAsync(connection, manifest);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"BLOCKED: {ex.Message}");
            return 1;
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "QuanLyNhaTro.csproj")))
            directory = directory.Parent;
        return directory?.FullName
               ?? throw new InvalidOperationException("Run from inside the QuanLyNhaTro repository.");
    }

    private static List<Migration> LoadManifest(string root)
    {
        var path = Path.Combine(root, "Database", "migration-manifest.json");
        return JsonSerializer.Deserialize<List<Migration>>(File.ReadAllText(path),
                   new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? throw new InvalidOperationException("Migration manifest is empty.");
    }

    private static void ValidateManifestFiles(string root, List<Migration> manifest)
    {
        if (manifest.Count == 0 || manifest.Select(x => x.Sequence).Distinct().Count() != manifest.Count
            || manifest.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count() != manifest.Count)
            throw new InvalidOperationException("Manifest sequences and ids must be unique.");
        for (var i = 0; i < manifest.Count; i++)
        {
            var migration = manifest.OrderBy(x => x.Sequence).ElementAt(i);
            if (migration.Sequence != i + 1)
                throw new InvalidOperationException("Manifest must be contiguous from sequence 1.");
            var path = Path.Combine(root, "Database", "updates", migration.File);
            var actual = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
            if (!actual.Equals(migration.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Checksum mismatch: {migration.File}. Update content and manifest intentionally.");
        }
    }

    private static async Task BootstrapAsync(MySqlConnection connection, List<Migration> manifest)
    {
        if (await JournalExistsAsync(connection))
            throw new InvalidOperationException("MigrationJournal already exists; bootstrap is one-time only.");

        var evidenced = new List<Migration>();
        foreach (var migration in manifest.OrderBy(x => x.Sequence))
        {
            var actual = await ReadScalarsAsync(connection, migration.EvidenceSql);
            if (!actual.SequenceEqual(migration.EvidenceExpected)) break;
            evidenced.Add(migration);
        }

        if (evidenced.Count == 0)
        {
            throw new InvalidOperationException(
                "No ordered migration prefix could be proven; journal was not created.");
        }

        foreach (var later in manifest.OrderBy(x => x.Sequence).Skip(evidenced.Count + 1))
        {
            var actual = await ReadScalarsAsync(connection, later.EvidenceSql);
            if (actual.SequenceEqual(later.EvidenceExpected))
                throw new InvalidOperationException(
                    $"Out-of-order schema evidence detected at {later.Id}; journal was not created.");
        }

        await EnsureJournalAsync(connection);
        foreach (var migration in evidenced)
        {
            await using var insert = new MySqlCommand("""
                INSERT INTO MigrationJournal(MigrationId,SequenceNo,Sha256,Source,Notes)
                VALUES(@Id,@Sequence,@Sha,'BootstrapEvidence','Verified against manifest evidence; migration SQL was not replayed.')
                """, connection);
            insert.Parameters.AddWithValue("@Id", migration.Id);
            insert.Parameters.AddWithValue("@Sequence", migration.Sequence);
            insert.Parameters.AddWithValue("@Sha", migration.Sha256);
            await insert.ExecuteNonQueryAsync();
        }
        Console.WriteLine($"Bootstrapped ordered prefix 1..{evidenced.Count} from schema evidence; migration SQL was not replayed.");
    }

    private static async Task ApplyNextAsync(MySqlConnection connection, string root, List<Migration> manifest)
    {
        if (!await JournalExistsAsync(connection))
            throw new InvalidOperationException("MigrationJournal is missing. Run bootstrap after a read-only evidence review.");

        var journal = await ReadJournalAsync(connection);
        ValidateJournal(manifest, journal);
        var baselineCoverage = journal.Where(x => x.Source == "FreshBaseline")
            .Select(x => x.Sequence).DefaultIfEmpty(0).Max();
        var next = manifest.OrderBy(x => x.Sequence)
            .FirstOrDefault(x => x.Sequence > baselineCoverage && journal.All(j => j.Sequence != x.Sequence));
        if (next == null)
        {
            Console.WriteLine("No pending migration.");
            return;
        }

        var covered = new HashSet<int>(journal.Select(x => x.Sequence));
        for (var sequence = baselineCoverage + 1; sequence < next.Sequence; sequence++)
            if (!covered.Contains(sequence))
                throw new InvalidOperationException($"Out-of-order gap at sequence {sequence}.");

        var script = await File.ReadAllTextAsync(Path.Combine(root, "Database", "updates", next.File));
        foreach (var statement in SplitMySqlScript(script))
        {
            await using var command = new MySqlCommand(statement, connection) { CommandTimeout = 300 };
            await command.ExecuteNonQueryAsync();
        }

        await using var insert = new MySqlCommand("""
            INSERT INTO MigrationJournal(MigrationId,SequenceNo,Sha256,Source,Notes)
            VALUES(@Id,@Sequence,@Sha,'Runner','Applied in manifest order after checksum verification.')
            """, connection);
        insert.Parameters.AddWithValue("@Id", next.Id);
        insert.Parameters.AddWithValue("@Sequence", next.Sequence);
        insert.Parameters.AddWithValue("@Sha", next.Sha256);
        await insert.ExecuteNonQueryAsync();
        Console.WriteLine($"Applied {next.Sequence}: {next.Id}");
    }

    private static async Task PrintStatusAsync(MySqlConnection connection, List<Migration> manifest)
    {
        if (!await JournalExistsAsync(connection))
        {
            Console.WriteLine("MigrationJournal: missing (status is read-only; use bootstrap only after approval). ");
            return;
        }

        var journal = await ReadJournalAsync(connection);
        ValidateJournal(manifest, journal);
        var baselineCoverage = journal.Where(x => x.Source == "FreshBaseline")
            .Select(x => x.Sequence).DefaultIfEmpty(0).Max();
        foreach (var migration in manifest.OrderBy(x => x.Sequence))
        {
            var entry = journal.FirstOrDefault(x => x.Sequence == migration.Sequence);
            var state = migration.Sequence <= baselineCoverage ? "covered-by-fresh-baseline"
                : entry == null ? "pending" : entry.Source;
            Console.WriteLine($"{migration.Sequence:00} {state,-25} {migration.Id}");
        }
    }

    private static void ValidateJournal(List<Migration> manifest, List<JournalRow> journal)
    {
        foreach (var row in journal.Where(x => x.Source != "FreshBaseline"))
        {
            var expected = manifest.SingleOrDefault(x => x.Sequence == row.Sequence && x.Id == row.Id)
                           ?? throw new InvalidOperationException($"Unknown or reordered journal row: {row.Sequence} {row.Id}.");
            if (!expected.Sha256.Equals(row.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Journal checksum mismatch: {row.Id}.");
        }
        var baseline = journal.Where(x => x.Source == "FreshBaseline").ToList();
        if (baseline.Any(x => x.Sequence > manifest.Max(m => m.Sequence)))
            throw new InvalidOperationException("Fresh baseline is newer than this manifest; use matching application code.");
    }

    private static async Task<bool> JournalExistsAsync(MySqlConnection connection)
    {
        await using var command = new MySqlCommand("""
            SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='MigrationJournal'
            """, connection);
        return Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
    }

    private static async Task EnsureJournalAsync(MySqlConnection connection)
    {
        await using var command = new MySqlCommand("""
            CREATE TABLE MigrationJournal (
                MigrationId VARCHAR(160) PRIMARY KEY,
                SequenceNo INT NOT NULL,
                Sha256 CHAR(64) NULL,
                AppliedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
                Source VARCHAR(30) NOT NULL,
                Notes VARCHAR(500) NULL,
                CONSTRAINT UQ_MigrationJournal_Sequence UNIQUE (SequenceNo),
                CONSTRAINT CK_MigrationJournal_Sequence CHECK (SequenceNo > 0),
                CONSTRAINT CK_MigrationJournal_Source CHECK (Source IN ('FreshBaseline','BootstrapEvidence','Runner'))
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
            """, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<List<JournalRow>> ReadJournalAsync(MySqlConnection connection)
    {
        var rows = new List<JournalRow>();
        await using var command = new MySqlCommand(
            "SELECT MigrationId,SequenceNo,Sha256,Source FROM MigrationJournal ORDER BY SequenceNo", connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            rows.Add(new JournalRow(reader.GetString(0), reader.GetInt32(1),
                reader.IsDBNull(2) ? null : reader.GetString(2), reader.GetString(3)));
        return rows;
    }

    private static async Task<List<long>> ReadScalarsAsync(MySqlConnection connection, string sql)
    {
        var values = new List<long>();
        await using var command = new MySqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        do
        {
            while (await reader.ReadAsync()) values.Add(Convert.ToInt64(reader.GetValue(0)));
        } while (await reader.NextResultAsync());
        return values;
    }

    internal static IEnumerable<string> SplitMySqlScript(string script)
    {
        var delimiter = ";";
        var statement = new StringBuilder();
        using var reader = new StringReader(script);
        while (reader.ReadLine() is { } line)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("DELIMITER ", StringComparison.OrdinalIgnoreCase))
            {
                delimiter = trimmed[10..].Trim();
                continue;
            }
            statement.AppendLine(line);
            if (!trimmed.EndsWith(delimiter, StringComparison.Ordinal)) continue;
            var sql = statement.ToString();
            sql = sql[..sql.LastIndexOf(delimiter, StringComparison.Ordinal)].Trim();
            statement.Clear();
            if (sql.Length > 0) yield return sql;
        }
        if (!string.IsNullOrWhiteSpace(statement.ToString()))
            yield return statement.ToString().Trim();
    }
}

internal sealed record Migration(
    int Sequence, string Id, string File, string Sha256,
    string EvidenceSql, long[] EvidenceExpected);
internal sealed record JournalRow(string Id, int Sequence, string? Sha256, string Source);
