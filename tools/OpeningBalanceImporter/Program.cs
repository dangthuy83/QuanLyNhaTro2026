using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using QuanLyNhaTro.Models;
using QuanLyNhaTro.Repositories;
using QuanLyNhaTro.Services;

return await OpeningBalanceImporter.RunAsync(args);

internal static class OpeningBalanceImporter
{
    private const string ConnectionVariable = "QUANLYNHATRO_CONNECTION";

    public static async Task<int> RunAsync(string[] args)
    {
        try
        {
            var options = Parse(args);
            var sourceHash = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(options.SourcePath)));
            var batch = JsonSerializer.Deserialize<MoSoImportBatch>(
                await File.ReadAllTextAsync(options.InputPath),
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
                }) ?? throw new InvalidOperationException("File input rong.");
            if (!sourceHash.Equals(batch.DotMoSo.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("SHA-256 file nguon khong khop input; khong duoc tiep tuc.");

            var connectionString = Environment.GetEnvironmentVariable(ConnectionVariable);
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException($"Set {ConnectionVariable}; importer khong doc appsettings.json.");

            var services = new ServiceCollection();
            services.AddScoped<IDbConnection>(_ => new MySqlConnection(connectionString));
            services.AddScoped<HopDongRepository>();
            services.AddScoped<HopDongKhachThueRepository>();
            services.AddScoped<PhongDichVuRepository>();
            services.AddScoped<HopDongDichVuRepository>();
            services.AddScoped<GiaoDichCocRepository>();
            services.AddScoped<CongNoSettlementService>();
            services.AddScoped<GiaoDichCocService>();
            services.AddScoped<PhongLifecycleService>();
            services.AddScoped<MoSoService>();
            await using var provider = services.BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();
            var importer = scope.ServiceProvider.GetRequiredService<MoSoService>();

            await importer.ValidateImportBatchAsync(batch);
            PrintSafeSummary("VALIDATE_PASS", sourceHash, batch);
            if (options.Command == "validate") return 0;
            if (!sourceHash.Equals(options.ConfirmSha, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("apply bat buoc --confirm-sha khop SHA-256 file nguon da validate.");

            if (options.FailureAfterContracts.HasValue
                && Environment.GetEnvironmentVariable("QUANLYNHATRO_REHEARSAL") != "1")
                throw new InvalidOperationException("Mo phong crash chi duoc phep khi QUANLYNHATRO_REHEARSAL=1.");
            var ids = await importer.ApplyImportBatchAsync(batch, options.FailureAfterContracts);
            Console.WriteLine($"APPLY_PASS source_sha256={sourceHash} contracts={ids.Count}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"BLOCKED: {ex.Message.Replace('\r', ' ').Replace('\n', ' ')}");
            return 1;
        }
    }

    private static void PrintSafeSummary(string state, string hash, MoSoImportBatch batch)
    {
        Console.WriteLine(
            $"{state} source_sha256={hash} contracts={batch.HopDong.Count} " +
            $"residencies={batch.HopDong.Sum(x => x.CuTru.Count)} " +
            $"services={batch.HopDong.Sum(x => x.DichVu.Count)} " +
            $"debts={batch.HopDong.Sum(x => x.CongNo.Count)} " +
            $"meters={batch.HopDong.Sum(x => x.ChiSo.Count)}");
    }

    private static ImportOptions Parse(string[] args)
    {
        var command = args.FirstOrDefault()?.ToLowerInvariant();
        if (command is not ("validate" or "apply")) throw Usage();
        string Value(string name)
        {
            var index = Array.IndexOf(args, name);
            if (index < 0 || index + 1 >= args.Length || string.IsNullOrWhiteSpace(args[index + 1]))
                throw Usage();
            return args[index + 1];
        }
        var input = Path.GetFullPath(Value("--input"));
        var source = Path.GetFullPath(Value("--source"));
        if (!File.Exists(input) || !File.Exists(source))
            throw new InvalidOperationException("Khong tim thay file input hoac file nguon.");
        var confirm = command == "apply" ? Value("--confirm-sha") : null;
        int? failureAfter = null;
        var failureIndex = Array.IndexOf(args, "--simulate-failure-after-contracts");
        if (failureIndex >= 0)
        {
            if (command != "apply" || failureIndex + 1 >= args.Length
                || !int.TryParse(args[failureIndex + 1], out var parsed) || parsed <= 0)
                throw Usage();
            failureAfter = parsed;
        }
        return new ImportOptions(command, input, source, confirm, failureAfter);
    }

    private static InvalidOperationException Usage() => new(
        "Usage: validate --input <json> --source <evidence-file> | " +
        "apply --input <json> --source <evidence-file> --confirm-sha <sha256>");
}

internal sealed record ImportOptions(
    string Command,
    string InputPath,
    string SourcePath,
    string? ConfirmSha,
    int? FailureAfterContracts);
