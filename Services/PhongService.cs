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
    PhongDichVuRepository phongDichVuRepo,
    PhongLifecycleService phongLifecycle)
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
        decimal[] donGias,
        bool dangSuaChua)
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
            var banGoc = await phongLifecycle.KhoaPhongAsync(conn, tx, phong.Id);
            var nhaIdYeuCau = phong.NhaId > 0 ? phong.NhaId : banGoc.NhaId;
            if (nhaIdYeuCau != banGoc.NhaId)
            {
                var nhaTonTai = await conn.ExecuteScalarAsync<bool>(
                    "SELECT EXISTS(SELECT 1 FROM Nha WHERE Id = @NhaId)",
                    new { NhaId = nhaIdYeuCau },
                    tx);
                if (!nhaTonTai)
                    throw new InvalidOperationException("Nhà đã chọn không tồn tại.");

                if (await CoDuLieuNghiepVuAsync(conn, tx, phong.Id))
                    throw new InvalidOperationException(
                        "Không thể đổi Nhà vì phòng đã có dữ liệu nghiệp vụ hoặc lịch sử. " +
                        "Hãy tạo phòng mới tại Nhà đích để bảo toàn lịch sử.");

                await phongRepo.UpdateNhaAsync(conn, tx, phong.Id, nhaIdYeuCau);
            }

            var coHopDongHieuLuc = await PhongLifecycleService.CoHopDongHieuLucTheoNgayAsync(
                conn, tx, phong.Id, DateTime.Today);
            var coHopDongTuongLai = await PhongLifecycleService.CoHopDongTuongLaiAsync(
                conn, tx, phong.Id, DateTime.Today);
            if (dangSuaChua && (coHopDongHieuLuc || coHopDongTuongLai))
                throw new InvalidOperationException(
                    "Không thể đặt phòng sang Đang sửa khi đang có hợp đồng hiệu lực hoặc hợp đồng tương lai.");

            phong.NhaId = nhaIdYeuCau;
            phong.TrangThai = coHopDongHieuLuc
                ? "DangThue"
                : dangSuaChua
                    ? "DangSuaChua"
                    : "Trong";
            await phongRepo.UpdateThongTinAsync(conn, tx, phong);
            await phongRepo.UpdateTrangThaiAsync(conn, tx, phong.Id, phong.TrangThai);
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

    public async Task<bool> CoDuLieuNghiepVuAsync(int phongId)
    {
        var conn = (MySqlConnection)db;
        if (conn.State != ConnectionState.Open) await conn.OpenAsync();
        return await CoDuLieuNghiepVuAsync(conn, null, phongId);
    }

    public async Task<PhongReconcileViewModel> ReconcileReadOnlyAsync(DateTime ngay)
    {
        const string sql = """
            SELECT
                p.Id AS PhongId,
                p.NhaId,
                n.TenNha,
                p.TenPhong,
                p.TrangThai AS TrangThaiSnapshot,
                (
                    SELECT COUNT(*) FROM HopDong hd
                    WHERE hd.PhongId = p.Id
                      AND hd.TrangThai IN ('ChoHieuLuc', 'DangHieuLuc')
                      AND hd.NgayBatDau <= @Ngay
                      AND (hd.NgayKetThuc IS NULL OR hd.NgayKetThuc >= @Ngay)
                ) AS SoHopDongHieuLuc,
                (
                    SELECT COUNT(*) FROM HopDong hd
                    WHERE hd.PhongId = p.Id
                      AND hd.TrangThai IN ('ChoHieuLuc', 'DangHieuLuc')
                      AND hd.NgayBatDau > @Ngay
                ) AS SoHopDongTuongLai,
                EXISTS(
                    SELECT 1
                    FROM HopDong h1
                    INNER JOIN HopDong h2
                        ON h2.PhongId = h1.PhongId AND h2.Id > h1.Id
                    WHERE h1.PhongId = p.Id
                      AND h1.TrangThai <> 'DaHuy'
                      AND h2.TrangThai <> 'DaHuy'
                      AND h1.NgayBatDau <= COALESCE(h2.NgayKetThuc, '9999-12-31')
                      AND COALESCE(h1.NgayKetThuc, '9999-12-31') >= h2.NgayBatDau
                ) AS CoOverlapHopDong,
                (
                    EXISTS(SELECT 1 FROM HopDong hd WHERE hd.PhongId = p.Id)
                  + EXISTS(SELECT 1 FROM ChiSoDienNuoc cs WHERE cs.PhongId = p.Id)
                  + EXISTS(SELECT 1 FROM ChiSoNgoaiHopDong csn WHERE csn.PhongId = p.Id)
                  + EXISTS(SELECT 1 FROM ThuChi tc WHERE tc.PhongId = p.Id)
                  + EXISTS(SELECT 1 FROM ChiSoDauChuyenDoiDichVu cd WHERE cd.PhongId = p.Id)
                  + EXISTS(
                        SELECT 1 FROM LichSuThayDoiGia ls
                        INNER JOIN PhongDichVu pdv ON pdv.Id = ls.DoiTuongId
                        WHERE ls.LoaiDoiTuong = 'DichVu' AND pdv.PhongId = p.Id)
                ) > 0 AS CoDuLieuNghiepVu
            FROM Phong p
            INNER JOIN Nha n ON n.Id = p.NhaId
            ORDER BY n.TenNha, p.TenPhong
            """;

        var rows = (await db.QueryAsync<PhongReconcileRow>(sql, new { Ngay = ngay.Date })).ToList();
        return new PhongReconcileViewModel
        {
            NgayDoiChieu = ngay.Date,
            Rows = rows
        };
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

    private static async Task<bool> CoDuLieuNghiepVuAsync(
        IDbConnection conn,
        IDbTransaction? tx,
        int phongId)
        => await conn.ExecuteScalarAsync<int>(
            """
            SELECT
                EXISTS(SELECT 1 FROM HopDong WHERE PhongId = @PhongId)
              + EXISTS(SELECT 1 FROM ChiSoDienNuoc WHERE PhongId = @PhongId)
              + EXISTS(SELECT 1 FROM ChiSoNgoaiHopDong WHERE PhongId = @PhongId)
              + EXISTS(SELECT 1 FROM ThuChi WHERE PhongId = @PhongId)
              + EXISTS(SELECT 1 FROM ChiSoDauChuyenDoiDichVu WHERE PhongId = @PhongId)
              + EXISTS(
                    SELECT 1
                    FROM LichSuThayDoiGia ls
                    INNER JOIN PhongDichVu pdv ON pdv.Id = ls.DoiTuongId
                    WHERE ls.LoaiDoiTuong = 'DichVu'
                      AND pdv.PhongId = @PhongId)
            """,
            new { PhongId = phongId },
            tx) > 0;

    private sealed class PhongDeletionBlockers
    {
        public int HopDong { get; set; }
        public int ChiSo { get; set; }
        public int ChiSoNgoai { get; set; }
        public int ThuChi { get; set; }
        public int Total => HopDong + ChiSo + ChiSoNgoai + ThuChi;
    }
}
