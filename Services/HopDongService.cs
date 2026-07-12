using System.Data;
using Dapper;
using MySqlConnector;
using QuanLyNhaTro.Models;
using QuanLyNhaTro.Repositories;

namespace QuanLyNhaTro.Services;

public class HopDongService(
    IDbConnection db,
    HopDongRepository hopDongRepo,
    HopDongKhachThueRepository hdKhachRepo,
    PhongDichVuRepository phongDichVuRepo,
    HopDongDichVuRepository hopDongDichVuRepo,
    PhongRepository phongRepo,
    GiaoDichCocService giaoDichCocService)
{
    public async Task HuyHopDongAsync(int hopDongId, DateTime ngayHuy)
    {
        var conn = (MySqlConnection)db;
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync();

        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            var hopDong = await conn.QueryFirstOrDefaultAsync<HopDong>(
                "SELECT * FROM HopDong WHERE Id = @Id FOR UPDATE",
                new { Id = hopDongId },
                tx) ?? throw new InvalidOperationException("Khong tim thay hop dong.");

            if (hopDong.TrangThai != "DangHieuLuc")
                throw new InvalidOperationException("Chi duoc huy hop dong dang hieu luc.");
            if (ngayHuy.Date >= hopDong.NgayBatDau.Date)
                throw new InvalidOperationException("Hop dong da den ngay bat dau. Hay dung flow Tra phong de quyet toan.");

            var coDuLieuNghiepVu = await conn.ExecuteScalarAsync<int>(
                """
                SELECT
                    EXISTS(SELECT 1 FROM HoaDon WHERE HopDongId = @Id)
                  + EXISTS(SELECT 1 FROM ChiSoDienNuoc WHERE HopDongId = @Id)
                  + EXISTS(SELECT 1 FROM GiaoDichCoc WHERE HopDongId = @Id)
                  + EXISTS(SELECT 1 FROM KhoanPhatSinhHopDong WHERE HopDongId = @Id)
                  + EXISTS(
                        SELECT 1
                        FROM ThanhToan tt
                        INNER JOIN HoaDon hd ON hd.Id = tt.HoaDonId
                        WHERE hd.HopDongId = @Id)
                """,
                new { Id = hopDongId },
                tx);

            if (coDuLieuNghiepVu > 0)
                throw new InvalidOperationException(
                    "Hop dong da co hoa don, chi so, coc, thanh toan hoac khoan phat sinh. Khong the huy.");

            await conn.ExecuteAsync(
                "UPDATE HopDong SET TrangThai = 'DaHuy' WHERE Id = @Id",
                new { Id = hopDongId },
                tx);

            var conHopDongChiemPhong = await conn.ExecuteScalarAsync<int>(
                """
                SELECT COUNT(*)
                FROM HopDong
                WHERE PhongId = @PhongId
                  AND Id <> @Id
                  AND TrangThai = 'DangHieuLuc'
                  AND NgayBatDau <= @Ngay
                  AND (NgayKetThuc IS NULL OR NgayKetThuc >= @Ngay)
                """,
                new { hopDong.PhongId, Id = hopDongId, Ngay = ngayHuy.Date },
                tx);

            if (conHopDongChiemPhong == 0)
                await phongRepo.UpdateTrangThaiAsync(conn, tx, hopDong.PhongId, "Trong");

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<int> TaoHopDongAsync(
        HopDong hopDong,
        int[] khachThueIds,
        int? khachChinhId,
        int[] phongDichVuIds)
    {
        hopDong.TrangThai = "DangHieuLuc";

        var conn = (MySqlConnection)db;
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync();

        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            var hopDongDangHieuLuc = await hopDongRepo.GetDangHieuLucByPhongAsync(conn, tx, hopDong.PhongId);
            if (hopDongDangHieuLuc != null)
                throw new InvalidOperationException($"Phong #{hopDong.PhongId} da co hop dong dang hieu luc.");

            var dichVuDaChon = await ValidateDichVuAsync(
                conn, tx, hopDong.PhongId, phongDichVuIds, requireActive: true);

            var hopDongId = await hopDongRepo.InsertAsync(conn, tx, hopDong);
            await CapNhatKhachThueAsync(conn, tx, hopDongId, khachThueIds, khachChinhId);
            await hopDongDichVuRepo.InsertManyAsync(
                conn,
                tx,
                hopDongId,
                dichVuDaChon.Select(x => x.Id),
                hopDong.NgayBatDau);
            await phongRepo.UpdateTrangThaiAsync(conn, tx, hopDong.PhongId, "DangThue");
            await giaoDichCocService.GhiNhanThuCocBanDauAsync(
                conn,
                tx,
                hopDongId,
                hopDong.TienCoc,
                hopDong.NgayBatDau,
                "Thu coc ban dau khi tao hop dong");

            await tx.CommitAsync();
            return hopDongId;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task CapNhatDichVuAsync(
        int hopDongId,
        int[] phongDichVuIds,
        int thangApDung,
        int namApDung)
    {
        if (thangApDung is < 1 or > 12 || namApDung < 2000)
            throw new InvalidOperationException("Ky ap dung khong hop le.");

        var hopDong = await hopDongRepo.GetByIdAsync(hopDongId)
            ?? throw new InvalidOperationException("Khong tim thay hop dong.");
        if (hopDong.TrangThai != "DangHieuLuc")
            throw new InvalidOperationException("Chi duoc thay doi dich vu cua hop dong dang hieu luc.");

        var ky = new DateTime(namApDung, thangApDung, 1);
        var kyBatDauHopDong = new DateTime(hopDong.NgayBatDau.Year, hopDong.NgayBatDau.Month, 1);
        if (ky < kyBatDauHopDong)
            throw new InvalidOperationException("Ky ap dung khong duoc truoc ky bat dau hop dong.");

        var conn = (MySqlConnection)db;
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync();

        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            var soHoaDon = await conn.ExecuteScalarAsync<int>(
                """
                SELECT COUNT(*)
                FROM HoaDon
                WHERE HopDongId = @HopDongId
                  AND (Nam > @Nam OR (Nam = @Nam AND Thang >= @Thang))
                """,
                new { HopDongId = hopDongId, Thang = thangApDung, Nam = namApDung },
                transaction: tx);
            if (soHoaDon > 0)
                throw new InvalidOperationException("Da co hoa don tu ky ap dung tro di. Khong the thay doi dich vu cho cac ky da chot.");

            var dichVuDaChon = await ValidateDichVuAsync(
                conn, tx, hopDong.PhongId, phongDichVuIds, requireActive: false);
            await hopDongDichVuRepo.ReplaceFromPeriodAsync(
                conn,
                tx,
                hopDongId,
                dichVuDaChon.Select(x => x.Id),
                ky);

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task SuaHopDongAsync(HopDong hopDong, int[] khachThueIds, int? khachChinhId)
    {
        var conn = (MySqlConnection)db;
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync();

        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            var giaGoc = await conn.ExecuteScalarAsync<decimal?>(
                "SELECT TienThueThoaThuan FROM HopDong WHERE Id = @Id FOR UPDATE",
                new { hopDong.Id },
                transaction: tx)
                ?? throw new InvalidOperationException("Khong tim thay hop dong.");
            hopDong.TienThueThoaThuan = giaGoc;

            await hopDongRepo.UpdateAsync(conn, tx, hopDong);
            await hdKhachRepo.DeleteByHopDongAsync(conn, tx, hopDong.Id);
            await CapNhatKhachThueAsync(conn, tx, hopDong.Id, khachThueIds, khachChinhId);

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private async Task CapNhatKhachThueAsync(
        IDbConnection conn,
        IDbTransaction tx,
        int hopDongId,
        int[] khachThueIds,
        int? khachChinhId)
    {
        foreach (var khachId in khachThueIds.Distinct())
        {
            await hdKhachRepo.InsertAsync(conn, tx, hopDongId, khachId, khachId == khachChinhId);
        }
    }

    private async Task<List<PhongDichVu>> ValidateDichVuAsync(
        IDbConnection conn,
        IDbTransaction tx,
        int phongId,
        IEnumerable<int> phongDichVuIds,
        bool requireActive)
    {
        var selectedIds = phongDichVuIds.Distinct().ToHashSet();
        var rows = await phongDichVuRepo.GetSelectedForPhongAsync(
            conn, tx, phongId, selectedIds, requireActive);
        if (rows.Count != selectedIds.Count)
            throw new InvalidOperationException("Danh sach dich vu co muc khong thuoc phong hoac da ngung ap dung.");

        var requiredIds = await phongDichVuRepo.GetRequiredIdsForPhongAsync(conn, tx, phongId);
        var missingRequired = requiredIds.Except(selectedIds).ToArray();
        if (missingRequired.Length > 0)
            throw new InvalidOperationException("Phai chon day du cac dich vu bat buoc cua phong.");

        return rows;
    }
}
