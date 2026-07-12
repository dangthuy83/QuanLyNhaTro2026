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

            if (hopDong.TrangThai is not ("ChoHieuLuc" or "DangHieuLuc"))
                throw new InvalidOperationException("Chi duoc huy hop dong cho hieu luc hoac dang hieu luc.");
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
        ValidateThongTinHopDong(hopDong, khachThueIds, khachChinhId);
        hopDong.TrangThai = hopDong.NgayBatDau.Date > DateTime.Today
            ? "ChoHieuLuc"
            : "DangHieuLuc";

        var conn = (MySqlConnection)db;
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync();

        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            var phongTonTai = await conn.ExecuteScalarAsync<int?>(
                "SELECT Id FROM Phong WHERE Id = @PhongId FOR UPDATE",
                new { hopDong.PhongId }, tx);
            if (!phongTonTai.HasValue)
                throw new InvalidOperationException("Khong tim thay phong.");
            if (await hopDongRepo.CoChongKhoangAsync(
                    conn, tx, hopDong.PhongId, hopDong.NgayBatDau, hopDong.NgayKetThuc))
                throw new InvalidOperationException("Phong da co hop dong chiem dung trong khoang thoi gian nay.");

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
            if (hopDong.TrangThai == "DangHieuLuc")
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
            var banGoc = await conn.QueryFirstOrDefaultAsync<HopDong>(
                "SELECT * FROM HopDong WHERE Id = @Id FOR UPDATE",
                new { hopDong.Id },
                transaction: tx)
                ?? throw new InvalidOperationException("Khong tim thay hop dong.");

            var coDuLieu = await CoDuLieuNghiepVuAsync(conn, tx, hopDong.Id);
            if (coDuLieu)
            {
                await hopDongRepo.UpdateGhiChuAsync(conn, tx, hopDong.Id, hopDong.GhiChu);
            }
            else
            {
                ValidateThongTinHopDong(hopDong, khachThueIds, khachChinhId);
                hopDong.PhongId = banGoc.PhongId;
                hopDong.TrangThai = banGoc.TrangThai;
                hopDong.TienThueThoaThuan = banGoc.TienThueThoaThuan;
                hopDong.HopDongTruocId = banGoc.HopDongTruocId;
                hopDong.DaXuLyChenhLechCoc = banGoc.DaXuLyChenhLechCoc;

                await conn.ExecuteScalarAsync<int>(
                    "SELECT Id FROM Phong WHERE Id = @PhongId FOR UPDATE",
                    new { banGoc.PhongId }, tx);
                if (await hopDongRepo.CoChongKhoangAsync(
                        conn, tx, banGoc.PhongId, hopDong.NgayBatDau, hopDong.NgayKetThuc, hopDong.Id))
                    throw new InvalidOperationException("Khoang thoi gian sua bi chong voi hop dong khac cua phong.");

                await hopDongRepo.UpdateEditableAsync(conn, tx, hopDong);
                await hdKhachRepo.DeleteByHopDongAsync(conn, tx, hopDong.Id);
                await CapNhatKhachThueAsync(conn, tx, hopDong.Id, khachThueIds, khachChinhId);
            }

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<int> KichHoatHopDongDenHanAsync(DateTime ngay)
    {
        var conn = (MySqlConnection)db;
        if (conn.State != ConnectionState.Open) await conn.OpenAsync();

        var ids = (await conn.QueryAsync<int>(
            "SELECT Id FROM HopDong WHERE TrangThai = 'ChoHieuLuc' AND NgayBatDau <= @Ngay ORDER BY NgayBatDau, Id",
            new { Ngay = ngay.Date })).ToArray();
        var count = 0;
        foreach (var id in ids)
        {
            await using var tx = await conn.BeginTransactionAsync();
            try
            {
                var hd = await conn.QueryFirstOrDefaultAsync<HopDong>(
                    "SELECT * FROM HopDong WHERE Id = @Id FOR UPDATE", new { Id = id }, tx);
                if (hd == null || hd.TrangThai != "ChoHieuLuc" || hd.NgayBatDau.Date > ngay.Date)
                {
                    await tx.RollbackAsync();
                    continue;
                }
                await conn.ExecuteScalarAsync<int>(
                    "SELECT Id FROM Phong WHERE Id = @PhongId FOR UPDATE", new { hd.PhongId }, tx);
                if (await hopDongRepo.CoChongKhoangAsync(
                        conn, tx, hd.PhongId, hd.NgayBatDau, hd.NgayKetThuc, hd.Id))
                {
                    await tx.RollbackAsync();
                    continue;
                }
                await conn.ExecuteAsync(
                    "UPDATE HopDong SET TrangThai = 'DangHieuLuc' WHERE Id = @Id", new { hd.Id }, tx);
                await phongRepo.UpdateTrangThaiAsync(conn, tx, hd.PhongId, "DangThue");
                await tx.CommitAsync();
                count++;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
        return count;
    }

    public async Task<bool> CoDuLieuNghiepVuAsync(int hopDongId)
    {
        var conn = (MySqlConnection)db;
        if (conn.State != ConnectionState.Open) await conn.OpenAsync();
        return await CoDuLieuNghiepVuAsync(conn, null, hopDongId);
    }

    private static void ValidateThongTinHopDong(HopDong hopDong, int[] khachThueIds, int? khachChinhId)
    {
        if (hopDong.NgayKetThuc.HasValue && hopDong.NgayKetThuc.Value.Date < hopDong.NgayBatDau.Date)
            throw new InvalidOperationException("Ngay ket thuc khong duoc truoc ngay bat dau.");
        var selected = khachThueIds.Distinct().ToHashSet();
        if (khachChinhId.HasValue && !selected.Contains(khachChinhId.Value))
            throw new InvalidOperationException("Khach dai dien phai thuoc danh sach khach da chon.");
    }

    private static async Task<bool> CoDuLieuNghiepVuAsync(IDbConnection conn, IDbTransaction? tx, int id)
        => await conn.ExecuteScalarAsync<int>(
            """
            SELECT
                EXISTS(SELECT 1 FROM HoaDon WHERE HopDongId = @Id)
              + EXISTS(SELECT 1 FROM ChiSoDienNuoc WHERE HopDongId = @Id)
              + EXISTS(SELECT 1 FROM GiaoDichCoc WHERE HopDongId = @Id)
              + EXISTS(SELECT 1 FROM KhoanPhatSinhHopDong WHERE HopDongId = @Id)
              + EXISTS(SELECT 1 FROM ThanhToan tt INNER JOIN HoaDon hd ON hd.Id = tt.HoaDonId WHERE hd.HopDongId = @Id)
            """, new { Id = id }, tx) > 0;

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
