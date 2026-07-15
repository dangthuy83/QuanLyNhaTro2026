using System.Data;
using Dapper;
using MySqlConnector;
using QuanLyNhaTro.Models;
using QuanLyNhaTro.Repositories;

namespace QuanLyNhaTro.Services;

public class ChiSoService(
    IDbConnection db,
    ChiSoDienNuocRepository chiSoRepo,
    MeterContinuityService continuity)
{
    public async Task LuuBatchAsync(IEnumerable<ChiSoDienNuoc> chiSos)
    {
        var items = chiSos.ToList();
        if (items.Count == 0) return;
        var conn = (MySqlConnection)db;
        if (conn.State != ConnectionState.Open) await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            var updateIds = items.Where(item => item.Id > 0).Select(item => item.Id).Distinct().ToArray();
            List<ChiSoScopeRow> persistedScopes = updateIds.Length == 0
                ? []
                : (await conn.QueryAsync<ChiSoScopeRow>(
                    "SELECT Id,PhongId FROM ChiSoDienNuoc WHERE Id IN @Ids",
                    new { Ids = updateIds }, tx)).ToList();
            var roomByMeterId = persistedScopes.ToDictionary(row => row.Id, row => row.PhongId);
            var roomIds = items.Select(item => item.Id > 0 && roomByMeterId.TryGetValue(item.Id, out var roomId)
                    ? roomId
                    : item.PhongId)
                .ToArray();
            await continuity.LockRoomsAsync(conn, tx, roomIds);

            foreach (var item in items)
            {
                await ValidateNgayDocAsync(conn, tx, item);
                if (item.LoaiGhiNhan == ChiSoDienNuoc.LoaiReset && string.IsNullOrWhiteSpace(item.LyDoDieuChinh))
                    throw new InvalidOperationException("Reset bat buoc co ly do dieu chinh.");
                _ = ChiSoConsumptionCalculator.Calculate(item);

                if (item.Id > 0)
                {
                    var current = await conn.QueryFirstOrDefaultAsync<ChiSoDienNuoc>(
                        "SELECT * FROM ChiSoDienNuoc WHERE Id = @Id FOR UPDATE", new { item.Id }, tx)
                        ?? throw new InvalidOperationException($"Khong tim thay chi so #{item.Id}.");
                    if (current.PhongId != item.PhongId || current.DichVuId != item.DichVuId ||
                        current.HopDongId != item.HopDongId || current.Thang != item.Thang || current.Nam != item.Nam)
                        throw new InvalidOperationException($"Chi so #{item.Id}: khong duoc thay doi phong, hop dong, dich vu hoac ky.");
                    if (await DaDuocDungTrenHoaDonAsync(conn, tx, item.Id))
                        throw new InvalidOperationException($"Chi so #{item.Id} da duoc dung tren hoa don. Hay xoa/reissue hoa don hop le truoc khi sua.");
                    await ValidateContinuityAsync(conn, tx, item, continuity);
                    await chiSoRepo.UpdateAsync(conn, tx, item);
                }
                else
                {
                    await ValidateContinuityAsync(conn, tx, item, continuity);
                    await chiSoRepo.InsertAsync(conn, tx, item);
                }
            }
            await tx.CommitAsync();
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            await tx.RollbackAsync();
            throw new InvalidOperationException("Batch co chi so trung phong, hop dong, dich vu va ky.", ex);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task XoaAsync(int id)
    {
        var preliminary = await chiSoRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Khong tim thay chi so #{id}.");
        var conn = (MySqlConnection)db;
        if (conn.State != ConnectionState.Open) await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            await continuity.LockRoomsAsync(conn, tx, [preliminary.PhongId]);
            var current = await conn.QueryFirstOrDefaultAsync<ChiSoDienNuoc>(
                "SELECT * FROM ChiSoDienNuoc WHERE Id = @Id FOR UPDATE", new { Id = id }, tx)
                ?? throw new KeyNotFoundException($"Khong tim thay chi so #{id}.");
            if (await DaDuocDungTrenHoaDonAsync(conn, tx, id))
                throw new InvalidOperationException($"Chi so #{id} da duoc dung tren hoa don, khong the xoa.");

            var eventDate = current.NgayDoc
                ?? throw new InvalidOperationException($"Chi so #{id} thieu ngay doc.");
            var next = await continuity.GetNextAsync(
                conn, tx,
                current.PhongId, current.DichVuId, eventDate,
                excludeMeterId: current.Id);
            if (next != null)
            {
                var invoiceNote = next.IsInvoiced ? " Moc phia sau da duoc dung tren hoa don." : string.Empty;
                throw new InvalidOperationException(
                    $"Khong the xoa chi so #{id} vi dang lam moc cho {next.Description}.{invoiceNote}");
            }

            await chiSoRepo.DeleteAsync(conn, tx, id);
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private static async Task ValidateNgayDocAsync(MySqlConnection conn, MySqlTransaction tx, ChiSoDienNuoc item)
    {
        if (!item.NgayDoc.HasValue) throw new InvalidOperationException("Ngay doc chi so la bat buoc.");
        var ngayDoc = item.NgayDoc.Value.Date;
        if (ngayDoc.Month != item.Thang || ngayDoc.Year != item.Nam)
            throw new InvalidOperationException($"Ngay doc {ngayDoc:dd/MM/yyyy} khong thuoc ky {item.Thang}/{item.Nam}.");
        if (!item.HopDongId.HasValue) return;
        var hopDong = await conn.QueryFirstOrDefaultAsync<HopDong>(
            "SELECT * FROM HopDong WHERE Id = @Id FOR UPDATE", new { Id = item.HopDongId.Value }, tx)
            ?? throw new InvalidOperationException($"Khong tim thay hop dong #{item.HopDongId.Value}.");
        if (hopDong.PhongId != item.PhongId)
            throw new InvalidOperationException("Chi so khong thuoc phong cua hop dong.");
        if (ngayDoc < hopDong.NgayBatDau.Date ||
            (hopDong.NgayKetThuc.HasValue && ngayDoc > hopDong.NgayKetThuc.Value.Date))
            throw new InvalidOperationException($"Ngay doc {ngayDoc:dd/MM/yyyy} nam ngoai thoi gian hieu luc hop dong.");
    }

    private static async Task<bool> DaDuocDungTrenHoaDonAsync(MySqlConnection conn, MySqlTransaction tx, int chiSoId)
        => (await conn.QueryAsync<int>(
            "SELECT Id FROM ChiTietHoaDon WHERE ChiSoDienNuocId = @Id FOR UPDATE",
            new { Id = chiSoId }, tx)).Any();

    private static async Task ValidateContinuityAsync(
        MySqlConnection conn,
        MySqlTransaction tx,
        ChiSoDienNuoc item,
        MeterContinuityService continuity)
    {
        var eventDate = item.NgayDoc
            ?? throw new InvalidOperationException("Ngay doc chi so la bat buoc.");
        var conversionStart = await continuity.GetConversionStartAsync(
            conn, tx, item.PhongId, item.DichVuId, item.Thang, item.Nam);
        await continuity.EnsureContinuityAsync(
            conn, tx,
            item.PhongId, item.DichVuId, eventDate,
            item.ChiSoDau, item.ChiSoCuoi,
            excludeMeterId: item.Id > 0 ? item.Id : null,
            authoritativeStart: conversionStart);
    }

    private sealed class ChiSoScopeRow
    {
        public int Id { get; init; }
        public int PhongId { get; init; }
    }
}
