using Dapper;
using MySqlConnector;
using System.Data;
using QuanLyNhaTro.Models;
using QuanLyNhaTro.Repositories;

namespace QuanLyNhaTro.Services;

/// <summary>Nghiệp vụ liên quan đến Phong: kiểm tra trạng thái, cập nhật khi ký/kết thúc hợp đồng.</summary>
public class PhongService(
    IDbConnection db,
    PhongRepository phongRepo,
    HopDongRepository hopDongRepo,
    PhongDichVuRepository phongDichVuRepo)
{
    public async Task<int> TaoPhongAsync(
        Phong phong,
        int[] dichVuIds,
        decimal[] donGias)
    {
        phong.TrangThai = "Trong";
        var prices = await BuildSelectedPricesAsync(dichVuIds, donGias);
        var conn = (MySqlConnection)db;
        if (conn.State != ConnectionState.Open) await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            var phongId = await phongRepo.InsertAsync(conn, tx, phong);
            await phongDichVuRepo.SyncForPhongAsync(conn, tx, phongId, prices);
            await tx.CommitAsync();
            return phongId;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task SuaPhongAsync(
        Phong phong,
        int[] dichVuIds,
        decimal[] donGias)
    {
        var prices = await BuildSelectedPricesAsync(dichVuIds, donGias);
        var existing = (await phongDichVuRepo.GetAllByPhongAsync(phong.Id))
            .ToDictionary(x => x.DichVuId);
        foreach (var dichVuId in prices.Keys.ToArray())
        {
            if (existing.TryGetValue(dichVuId, out var pdv))
                prices[dichVuId] = pdv.DonGia;
        }
        var conn = (MySqlConnection)db;
        if (conn.State != ConnectionState.Open) await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            await phongRepo.UpdateAsync(conn, tx, phong);
            await phongDichVuRepo.SyncForPhongAsync(conn, tx, phong.Id, prices);
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task XoaPhongAsync(int phongId)
    {
        var conn = (MySqlConnection)db;
        if (conn.State != ConnectionState.Open) await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            var blockers = await conn.QuerySingleAsync<PhongDeletionBlockers>(
                """
                SELECT
                    (SELECT COUNT(*) FROM HopDong WHERE PhongId = @PhongId) AS HopDong,
                    (SELECT COUNT(*) FROM ChiSoDienNuoc WHERE PhongId = @PhongId) AS ChiSo,
                    (SELECT COUNT(*) FROM ChiSoNgoaiHopDong WHERE PhongId = @PhongId) AS ChiSoNgoai,
                    (SELECT COUNT(*) FROM ThuChi WHERE PhongId = @PhongId) AS ThuChi
                """,
                new { PhongId = phongId },
                transaction: tx);
            if (blockers.Total > 0)
                throw new InvalidOperationException(
                    $"Khong the xoa phong vi da co du lieu nghiep vu: " +
                    $"{blockers.HopDong} hop dong, {blockers.ChiSo} chi so, " +
                    $"{blockers.ChiSoNgoai} chi so ngoai hop dong, {blockers.ThuChi} thu/chi.");

            var phongDichVuIds = (await conn.QueryAsync<int>(
                "SELECT Id FROM PhongDichVu WHERE PhongId = @PhongId",
                new { PhongId = phongId },
                transaction: tx)).ToArray();
            if (phongDichVuIds.Length > 0)
            {
                await conn.ExecuteAsync(
                    "DELETE FROM LichSuThayDoiGia WHERE LoaiDoiTuong = 'DichVu' AND DoiTuongId IN @Ids",
                    new { Ids = phongDichVuIds },
                    transaction: tx);
            }
            await conn.ExecuteAsync(
                "DELETE FROM PhongDichVu WHERE PhongId = @PhongId",
                new { PhongId = phongId },
                transaction: tx);
            await conn.ExecuteAsync(
                "DELETE FROM Phong WHERE Id = @PhongId",
                new { PhongId = phongId },
                transaction: tx);

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    /// <summary>Sau khi ký hợp đồng mới → cập nhật phòng sang DangThue.</summary>
    public async Task XuLyKyHopDongAsync(int phongId)
        => await phongRepo.UpdateTrangThaiAsync(phongId, "DangThue");

    /// <summary>
    /// Sau khi kết thúc / huỷ hợp đồng:
    /// kiểm tra không còn hợp đồng DangHieuLuc nào thì chuyển phòng về Trong.
    /// </summary>
    public async Task XuLyKetThucHopDongAsync(int phongId)
    {
        var con = await hopDongRepo.GetDangHieuLucByPhongAsync(phongId);
        if (con == null)
            await phongRepo.UpdateTrangThaiAsync(phongId, "Trong");
    }

    /// <summary>Tính tổng nợ còn lại của hợp đồng (dùng khi trả phòng, hoàn cọc).</summary>
    public static decimal TinhTienHoanCoc(HopDong hopDong, decimal tongNoCuoiKy)
        => hopDong.TienCoc - tongNoCuoiKy;

    private async Task<Dictionary<int, decimal>> BuildSelectedPricesAsync(
        int[] dichVuIds,
        decimal[] donGias)
    {
        var result = new Dictionary<int, decimal>();
        for (var i = 0; i < dichVuIds.Length; i++)
        {
            var price = i < donGias.Length ? donGias[i] : 0;
            if (price < 0)
                throw new InvalidOperationException("Don gia dich vu khong duoc am.");
            result[dichVuIds[i]] = price;
        }

        var requiredIds = (await db.QueryAsync<int>(
            "SELECT Id FROM DichVu WHERE BatBuocKhiThue = 1")).ToHashSet();
        if (requiredIds.Except(result.Keys).Any())
            throw new InvalidOperationException("Phai chon day du cac dich vu bat buoc khi tao hoac sua phong.");
        return result;
    }

    private sealed class PhongDeletionBlockers
    {
        public int HopDong { get; set; }
        public int ChiSo { get; set; }
        public int ChiSoNgoai { get; set; }
        public int ThuChi { get; set; }
        public int Total => HopDong + ChiSo + ChiSoNgoai + ThuChi;
    }
}
