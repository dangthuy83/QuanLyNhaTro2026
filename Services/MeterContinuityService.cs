using System.Data;
using Dapper;
using MySqlConnector;
using QuanLyNhaTro.Models;

namespace QuanLyNhaTro.Services;

public sealed class MeterContinuityService(IDbConnection db)
{
    private const string EventCte = """
        WITH MeterEvents AS (
            SELECT cs.PhongId,
                   cs.DichVuId,
                   cs.NgayDoc AS EventDate,
                   'HopDong' AS SourceType,
                   cs.Id AS SourceId,
                   cs.HopDongId,
                   cs.ChiSoDau AS StartReading,
                   cs.ChiSoCuoi AS EndReading,
                   CASE WHEN EXISTS (
                       SELECT 1 FROM ChiTietHoaDon ct WHERE ct.ChiSoDienNuocId = cs.Id
                   ) THEN 1 ELSE 0 END AS IsInvoiced
            FROM ChiSoDienNuoc cs
            WHERE cs.PhongId = @PhongId
              AND cs.DichVuId = @DichVuId
              AND (@ExcludeMeterId IS NULL OR cs.Id <> @ExcludeMeterId)
              AND (@ExcludeHopDongId IS NULL OR cs.HopDongId IS NULL OR cs.HopDongId <> @ExcludeHopDongId)

            UNION ALL

            SELECT cs.PhongId,
                   cs.DichVuId,
                   cs.NgayGhiNhan AS EventDate,
                   'NgoaiHopDong' AS SourceType,
                   cs.Id AS SourceId,
                   NULL AS HopDongId,
                   cs.TuChiSo AS StartReading,
                   cs.DenChiSo AS EndReading,
                   0 AS IsInvoiced
            FROM ChiSoNgoaiHopDong cs
            WHERE cs.PhongId = @PhongId
              AND cs.DichVuId = @DichVuId
              AND (@ExcludeOffContractId IS NULL OR cs.Id <> @ExcludeOffContractId)

            UNION ALL

            SELECT cs.PhongId,
                   cs.DichVuId,
                   cs.NgayChot AS EventDate,
                   'MoSo' AS SourceType,
                   cs.Id AS SourceId,
                   cs.HopDongId,
                   cs.ChiSo AS StartReading,
                   cs.ChiSo AS EndReading,
                   0 AS IsInvoiced
            FROM ChiSoMoSo cs
            WHERE cs.PhongId = @PhongId
              AND cs.DichVuId = @DichVuId
        )
        """;

    public async Task LockRoomsAsync(
        MySqlConnection conn,
        MySqlTransaction tx,
        IEnumerable<int> roomIds)
    {
        foreach (var roomId in roomIds.Distinct().OrderBy(id => id))
        {
            var locked = await conn.ExecuteScalarAsync<int?>(
                "SELECT Id FROM Phong WHERE Id=@Id FOR UPDATE",
                new { Id = roomId }, tx);
            if (!locked.HasValue)
                throw new InvalidOperationException($"Khong tim thay phong #{roomId}.");
        }
    }

    public async Task<MeterReadingAnchor?> GetLatestAsync(int roomId, int serviceId)
    {
        var conn = (MySqlConnection)db;
        if (conn.State != ConnectionState.Open) await conn.OpenAsync();
        return await QueryLatestAsync(conn, null, roomId, serviceId, null, null, null, null);
    }

    public async Task<MeterReadingAnchor?> GetPreviousOnOrBeforeAsync(
        int roomId,
        int serviceId,
        DateTime cutoffDate,
        int? excludeContractId = null)
    {
        var conn = (MySqlConnection)db;
        if (conn.State != ConnectionState.Open) await conn.OpenAsync();
        return await QueryLatestAsync(
            conn, null, roomId, serviceId, cutoffDate.Date, null, null, excludeContractId);
    }

    public async Task<MeterReadingAnchor?> GetOpeningAsync(int contractId, int serviceId)
    {
        var conn = (MySqlConnection)db;
        if (conn.State != ConnectionState.Open) await conn.OpenAsync();
        return await conn.QueryFirstOrDefaultAsync<MeterReadingAnchor>(
            """
            SELECT PhongId,DichVuId,NgayChot EventDate,'MoSo' SourceType,Id SourceId,
                   HopDongId,ChiSo StartReading,ChiSo EndReading,0 IsInvoiced
            FROM ChiSoMoSo
            WHERE HopDongId=@HopDongId AND DichVuId=@DichVuId
            """, new { HopDongId = contractId, DichVuId = serviceId });
    }

    public async Task EnsureContinuityAsync(
        MySqlConnection conn,
        MySqlTransaction tx,
        int roomId,
        int serviceId,
        DateTime eventDate,
        decimal startReading,
        decimal endReading,
        int? excludeMeterId = null,
        int? excludeOffContractId = null,
        decimal? authoritativeStart = null)
    {
        var sameDateCount = await CountSameDateAsync(
            conn, tx, roomId, serviceId, eventDate, excludeMeterId, excludeOffContractId);
        if (sameDateCount > 0)
            throw new InvalidOperationException(
                $"Da co moc chi so khac cua phong/dich vu vao ngay {eventDate:dd/MM/yyyy}. Khong the xac dinh thu tu chuoi trong cung mot ngay.");

        var previous = await QueryLatestAsync(
            conn, tx, roomId, serviceId, eventDate.Date, false,
            excludeMeterId, null, excludeOffContractId);
        var expectedStart = authoritativeStart ?? previous?.EndReading;
        if (expectedStart.HasValue && startReading != expectedStart.Value)
        {
            var source = authoritativeStart.HasValue
                ? "moc chi so dau chuyen doi"
                : previous!.Description;
            throw new InvalidOperationException(
                $"Chi so tu/dau {startReading:N2} phai noi dung moc {expectedStart.Value:N2} tu {source}. Khong cho phep gap chi so ngam.");
        }

        var next = await QueryNextAsync(
            conn, tx, roomId, serviceId, eventDate.Date, excludeMeterId, excludeOffContractId);
        if (next != null && endReading != next.StartReading)
            throw new InvalidOperationException(
                $"Chi so den/cuoi {endReading:N2} phai noi dung chi so dau {next.StartReading:N2} cua {next.Description}. Khong the chen doan lam thay doi du lieu phia sau.");
    }

    public async Task<MeterReadingAnchor?> GetNextAsync(
        MySqlConnection conn,
        MySqlTransaction tx,
        int roomId,
        int serviceId,
        DateTime eventDate,
        int? excludeMeterId = null,
        int? excludeOffContractId = null)
        => await QueryNextAsync(
            conn, tx, roomId, serviceId, eventDate.Date, excludeMeterId, excludeOffContractId);

    public async Task<decimal?> GetConversionStartAsync(
        MySqlConnection conn,
        MySqlTransaction tx,
        int roomId,
        int serviceId,
        int month,
        int year)
        => await conn.ExecuteScalarAsync<decimal?>("""
            SELECT cs.ChiSoDau
            FROM ChiSoDauChuyenDoiDichVu cs
            INNER JOIN LichSuHinhThucDichVu ls ON ls.Id=cs.LichSuHinhThucDichVuId
            WHERE ls.DichVuId=@DichVuId
              AND cs.PhongId=@PhongId
              AND ls.KyApDung=@Ky
            """,
            new
            {
                PhongId = roomId,
                DichVuId = serviceId,
                Ky = new DateTime(year, month, 1)
            }, tx);

    private static async Task<int> CountSameDateAsync(
        MySqlConnection conn,
        MySqlTransaction tx,
        int roomId,
        int serviceId,
        DateTime eventDate,
        int? excludeMeterId,
        int? excludeOffContractId)
        => await conn.ExecuteScalarAsync<int>("""
            SELECT
                (SELECT COUNT(*) FROM ChiSoDienNuoc
                 WHERE PhongId=@PhongId AND DichVuId=@DichVuId AND NgayDoc=@EventDate
                   AND (@ExcludeMeterId IS NULL OR Id<>@ExcludeMeterId))
              + (SELECT COUNT(*) FROM ChiSoNgoaiHopDong
                 WHERE PhongId=@PhongId AND DichVuId=@DichVuId AND NgayGhiNhan=@EventDate
                   AND (@ExcludeOffContractId IS NULL OR Id<>@ExcludeOffContractId))
              + (SELECT COUNT(*) FROM ChiSoMoSo
                 WHERE PhongId=@PhongId AND DichVuId=@DichVuId AND NgayChot=@EventDate)
            """,
            new
            {
                PhongId = roomId,
                DichVuId = serviceId,
                EventDate = eventDate.Date,
                ExcludeMeterId = excludeMeterId,
                ExcludeOffContractId = excludeOffContractId
            }, tx);

    private static async Task<MeterReadingAnchor?> QueryLatestAsync(
        MySqlConnection conn,
        MySqlTransaction? tx,
        int roomId,
        int serviceId,
        DateTime? cutoffDate,
        bool? inclusive,
        int? excludeMeterId,
        int? excludeContractId,
        int? excludeOffContractId = null)
    {
        var datePredicate = cutoffDate.HasValue
            ? inclusive == false ? "EventDate < @CutoffDate" : "EventDate <= @CutoffDate"
            : "1=1";
        var sql = EventCte + $"""
            SELECT *
            FROM MeterEvents
            WHERE {datePredicate}
            ORDER BY EventDate DESC,
                     CASE SourceType WHEN 'NgoaiHopDong' THEN 1 ELSE 0 END DESC,
                     SourceId DESC
            LIMIT 1
            """;
        return await conn.QueryFirstOrDefaultAsync<MeterReadingAnchor>(
            sql,
            new
            {
                PhongId = roomId,
                DichVuId = serviceId,
                CutoffDate = cutoffDate?.Date,
                ExcludeMeterId = excludeMeterId,
                ExcludeHopDongId = excludeContractId,
                ExcludeOffContractId = excludeOffContractId
            }, tx);
    }

    private static async Task<MeterReadingAnchor?> QueryNextAsync(
        MySqlConnection conn,
        MySqlTransaction tx,
        int roomId,
        int serviceId,
        DateTime eventDate,
        int? excludeMeterId,
        int? excludeOffContractId)
    {
        var sql = EventCte + """
            SELECT *
            FROM MeterEvents
            WHERE EventDate > @CutoffDate
            ORDER BY EventDate,
                     CASE SourceType WHEN 'HopDong' THEN 0 ELSE 1 END,
                     SourceId
            LIMIT 1
            """;
        return await conn.QueryFirstOrDefaultAsync<MeterReadingAnchor>(
            sql,
            new
            {
                PhongId = roomId,
                DichVuId = serviceId,
                CutoffDate = eventDate.Date,
                ExcludeMeterId = excludeMeterId,
                ExcludeHopDongId = (int?)null,
                ExcludeOffContractId = excludeOffContractId
            }, tx);
    }
}

public sealed class MeterReadingAnchor
{
    public int PhongId { get; init; }
    public int DichVuId { get; init; }
    public DateTime EventDate { get; init; }
    public string SourceType { get; init; } = string.Empty;
    public int SourceId { get; init; }
    public int? HopDongId { get; init; }
    public decimal StartReading { get; init; }
    public decimal EndReading { get; init; }
    public bool IsInvoiced { get; init; }

    public string Description => SourceType switch
    {
        "NgoaiHopDong" => $"chi so ngoai hop dong #{SourceId} ngay {EventDate:dd/MM/yyyy}",
        "MoSo" => $"chi so mo so #{SourceId} ngay {EventDate:dd/MM/yyyy}",
        _ => $"chi so hop dong #{SourceId} ngay {EventDate:dd/MM/yyyy}"
    };
}
