using System.Data;
using Dapper;
using MySqlConnector;
using QuanLyNhaTro.Models;
using QuanLyNhaTro.Repositories;

namespace QuanLyNhaTro.Services;

public sealed class ChiSoNgoaiHopDongService(
    IDbConnection db,
    ChiSoNgoaiHopDongRepository repository,
    MeterContinuityService continuity)
{
    public async Task<int> CreateAsync(ChiSoNgoaiHopDong item)
    {
        NormalizeAndValidate(item);

        var conn = (MySqlConnection)db;
        if (conn.State != ConnectionState.Open) await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            await continuity.LockRoomsAsync(conn, tx, [item.PhongId]);
            await ValidateServiceAndDateAsync(conn, tx, item);
            await continuity.EnsureContinuityAsync(
                conn, tx,
                item.PhongId, item.DichVuId, item.NgayGhiNhan.Date,
                item.TuChiSo, item.DenChiSo);

            var id = await repository.InsertAsync(conn, tx, item);
            await tx.CommitAsync();
            return id;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        var preliminary = await repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Khong tim thay chi so ngoai hop dong #{id}.");

        var conn = (MySqlConnection)db;
        if (conn.State != ConnectionState.Open) await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            await continuity.LockRoomsAsync(conn, tx, [preliminary.PhongId]);
            var current = await conn.QueryFirstOrDefaultAsync<ChiSoNgoaiHopDong>(
                "SELECT * FROM ChiSoNgoaiHopDong WHERE Id=@Id FOR UPDATE",
                new { Id = id }, tx)
                ?? throw new KeyNotFoundException($"Khong tim thay chi so ngoai hop dong #{id}.");

            var next = await continuity.GetNextAsync(
                conn, tx,
                current.PhongId, current.DichVuId, current.NgayGhiNhan,
                excludeOffContractId: current.Id);
            if (next != null)
            {
                var invoiceNote = next.IsInvoiced ? " Moc phia sau da duoc dung tren hoa don." : string.Empty;
                throw new InvalidOperationException(
                    $"Khong the xoa vi dong nay dang lam moc cho {next.Description}.{invoiceNote}");
            }

            await repository.DeleteAsync(conn, tx, id);
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private static void NormalizeAndValidate(ChiSoNgoaiHopDong item)
    {
        item.NgayGhiNhan = item.NgayGhiNhan.Date;
        item.LyDo = item.LyDo?.Trim() ?? string.Empty;
        item.GhiChu = string.IsNullOrWhiteSpace(item.GhiChu) ? null : item.GhiChu.Trim();
        item.LoaiGhiNhan = string.IsNullOrWhiteSpace(item.LoaiGhiNhan)
            ? ChiSoNgoaiHopDong.LoaiBinhThuong
            : item.LoaiGhiNhan.Trim();

        if (item.PhongId <= 0) throw new InvalidOperationException("Phong khong hop le.");
        if (item.DichVuId <= 0) throw new InvalidOperationException("Dich vu khong hop le.");
        if (!BusinessDataLimits.IsValidBusinessDate(item.NgayGhiNhan))
            throw new InvalidOperationException("Ngay ghi nhan khong hop le.");
        if (item.TuChiSo < 0 || item.DenChiSo < 0)
            throw new InvalidOperationException("Chi so khong duoc am.");
        if (string.IsNullOrWhiteSpace(item.LyDo))
            throw new InvalidOperationException("Ly do su dung ngoai hop dong la bat buoc.");

        if (item.LoaiGhiNhan == ChiSoNgoaiHopDong.LoaiBinhThuong)
        {
            item.ChiSoTruocReset = null;
            item.ChiSoSauReset = null;
            item.LyDoDieuChinh = null;
        }
        else
        {
            item.LyDoDieuChinh = string.IsNullOrWhiteSpace(item.LyDoDieuChinh)
                ? null
                : item.LyDoDieuChinh.Trim();
        }

        _ = ChiSoConsumptionCalculator.Calculate(item);
    }

    private static async Task ValidateServiceAndDateAsync(
        MySqlConnection conn,
        MySqlTransaction tx,
        ChiSoNgoaiHopDong item)
    {
        var service = await conn.QueryFirstOrDefaultAsync<DichVu>(
            "SELECT * FROM DichVu WHERE Id=@Id",
            new { Id = item.DichVuId }, tx)
            ?? throw new InvalidOperationException("Dich vu khong hop le.");

        var period = new DateTime(item.NgayGhiNhan.Year, item.NgayGhiNhan.Month, 1);
        var effectiveType = await conn.ExecuteScalarAsync<string?>("""
            SELECT COALESCE(
                (SELECT LoaiTinhPhiMoi FROM LichSuHinhThucDichVu
                 WHERE DichVuId=@DichVuId AND KyApDung<=@Ky
                 ORDER BY KyApDung DESC LIMIT 1),
                (SELECT LoaiTinhPhiCu FROM LichSuHinhThucDichVu
                 WHERE DichVuId=@DichVuId
                 ORDER BY KyApDung LIMIT 1),
                @FallbackType)
            """,
            new
            {
                DichVuId = item.DichVuId,
                Ky = period,
                FallbackType = service.LoaiTinhPhi
            }, tx);
        if (effectiveType != DichVu.LoaiTheoChiSo)
            throw new InvalidOperationException("Dich vu khong phai loai TheoChiSo tai ky cua ngay ghi nhan.");

        var contractId = await conn.ExecuteScalarAsync<int?>("""
            SELECT Id
            FROM HopDong
            WHERE PhongId=@PhongId
              AND TrangThai<>'DaHuy'
              AND @Ngay BETWEEN NgayBatDau AND COALESCE(NgayKetThuc,'9999-12-31')
            ORDER BY Id
            LIMIT 1
            """,
            new { item.PhongId, Ngay = item.NgayGhiNhan }, tx);
        if (contractId.HasValue)
            throw new InvalidOperationException(
                $"Ngay ghi nhan dang thuoc hop dong #{contractId}. Hay nhap chi so theo hop dong.");
    }
}
