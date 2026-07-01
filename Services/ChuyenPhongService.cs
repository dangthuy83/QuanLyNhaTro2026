using Dapper;
using MySqlConnector;
using QuanLyNhaTro.Models;
using QuanLyNhaTro.Repositories;

namespace QuanLyNhaTro.Services;

public class ChuyenPhongService(
    IConfiguration config,
    HopDongRepository hopDongRepo,
    HoaDonRepository hoaDonRepo,
    PhongRepository phongRepo,
    PhongDichVuRepository phongDvRepo,
    LichSuThayDoiGiaRepository lichSuRepo,
    GiaoDichCocService giaoDichCocService,
    CongNoSettlementService congNoSettlementService)
{
    public async Task<(int HopDongMoiId, int HoaDonCuId, int HoaDonMoiId)> ThucHienAsync(
        ChuyenPhongViewModel vm)
    {
        var hdCu = await hopDongRepo.GetByIdAsync(vm.HopDongCuId)
            ?? throw new InvalidOperationException("Khong tim thay hop dong.");

        var phongCu = await phongRepo.GetByIdAsync(hdCu.PhongId)
            ?? throw new InvalidOperationException("Khong tim thay phong cu.");

        if (vm.NgayChuyenDi.Date < hdCu.NgayBatDau.Date)
            throw new InvalidOperationException("Ngay chuyen phong khong duoc truoc ngay bat dau hop dong cu.");

        int thang = vm.NgayChuyenDi.Month;
        int nam = vm.NgayChuyenDi.Year;
        int soNgayTrongThang = BillingPeriodCalculator.GetDaysInMonth(thang, nam);
        int soNgayOCu = BillingPeriodCalculator.CountOccupiedDays(thang, nam, hdCu.NgayBatDau, vm.NgayChuyenDi);
        int soNgayOMoi = BillingPeriodCalculator.CountOccupiedDays(thang, nam, vm.NgayBatDauMoi, null);

        if (soNgayOCu <= 0)
            throw new InvalidOperationException("Hop dong cu khong co ngay o trong ky chuyen phong.");

        if (soNgayOMoi <= 0)
            throw new InvalidOperationException("Ngay chuyen phong la ngay cuoi thang; phong moi khong phat sinh ngay o trong ky nay.");

        decimal giaPhongCu = await LayGiaPhongAsync(hdCu, thang, nam);
        decimal tienPhongCu = BillingPeriodCalculator.CalculateRoomCharge(giaPhongCu, soNgayOCu, soNgayTrongThang);
        decimal tienPhongMoi = BillingPeriodCalculator.CalculateRoomCharge(vm.TienThueMoi, soNgayOMoi, soNgayTrongThang);

        var dvCu = (await phongDvRepo.GetByPhongAsync(hdCu.PhongId)).ToList();
        var dvMoi = (await phongDvRepo.GetByPhongAsync(vm.PhongMoiId)).ToList();

        decimal noXuyen = await hoaDonRepo.GetTongNoConLaiAsync(vm.HopDongCuId);

        var conn = new MySqlConnection(config.GetConnectionString("DefaultConnection"));
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();

        try
        {
            await conn.ExecuteAsync(
                "UPDATE HopDong SET TrangThai='DaChuyenPhong', NgayKetThuc=@Ngay WHERE Id=@Id",
                new { Ngay = vm.NgayChuyenDi, Id = vm.HopDongCuId }, tx);

            await conn.ExecuteAsync(
                "UPDATE Phong SET TrangThai='Trong' WHERE Id=@Id",
                new { Id = hdCu.PhongId }, tx);

            var hdMoiId = await conn.ExecuteScalarAsync<int>("""
                INSERT INTO HopDong
                    (PhongId, NgayBatDau, TienThueThoaThuan, TienCoc,
                     TrangThai, HopDongTruocId, DaXuLyChenhLechCoc, GhiChu, NgayTao)
                VALUES
                    (@PhongId, @NgayBatDau, @TienThue, @TienCoc,
                     'DangHieuLuc', @HopDongTruocId,
                     @CanXuLyCoc, @GhiChu, NOW());
                SELECT LAST_INSERT_ID();
                """,
                new
                {
                    PhongId = vm.PhongMoiId,
                    NgayBatDau = vm.NgayBatDauMoi,
                    TienThue = vm.TienThueMoi,
                    TienCoc = vm.TienCocMoi,
                    HopDongTruocId = vm.HopDongCuId,
                    CanXuLyCoc = vm.TienCocMoi != hdCu.TienCoc,
                    GhiChu = $"Chuyen tu {phongCu.TenPhong} ngay {vm.NgayChuyenDi:dd/MM/yyyy}"
                }, tx);

            await conn.ExecuteAsync("""
                INSERT INTO HopDongKhachThue (HopDongId, KhachThueId, LaDaiDien)
                SELECT @MoiId, KhachThueId, LaDaiDien
                FROM HopDongKhachThue WHERE HopDongId = @CuId
                """,
                new { MoiId = hdMoiId, CuId = vm.HopDongCuId }, tx);

            await conn.ExecuteAsync(
                "UPDATE Phong SET TrangThai='DangThue' WHERE Id=@Id",
                new { Id = vm.PhongMoiId }, tx);

            var chiTietDvCu = await TinhChiTietDichVuAsync(
                conn, tx, hdCu.PhongId, vm.HopDongCuId, dvCu.Where(d => d.DichVu?.LoaiTinhPhi == "TheoChiSo"), thang, nam);
            decimal tongDvCu = chiTietDvCu.Sum(d => d.ThanhTien);
            decimal tongCongCu = tienPhongCu + tongDvCu;

            var hdCuId = await conn.ExecuteScalarAsync<int>("""
                INSERT INTO HoaDon
                    (HopDongId, Thang, Nam, NgayLap, TienPhong, TongTienDichVu,
                     TongCong, SoTienDaThu, TrangThaiThanhToan,
                     SoNgayO, SoNgayTrongThang, TienNoKyTruoc)
                VALUES
                    (@HopDongId, @Thang, @Nam, NOW(), @TienPhong, @TongDV,
                     @TongCong, 0, 'ChuaThu',
                     @SoNgayO, @SoNgayTrongThang, 0);
                SELECT LAST_INSERT_ID();
                """,
                new
                {
                    HopDongId = vm.HopDongCuId,
                    Thang = thang,
                    Nam = nam,
                    TienPhong = tienPhongCu,
                    TongDV = tongDvCu,
                    TongCong = tongCongCu,
                    SoNgayO = soNgayOCu,
                    SoNgayTrongThang = soNgayTrongThang
                }, tx);

            await InsertChiTietAsync(conn, tx, hdCuId, chiTietDvCu);

            var chiTietDvMoi = await TinhChiTietDichVuAsync(conn, tx, vm.PhongMoiId, hdMoiId, dvMoi, thang, nam);
            decimal tongDvMoi = chiTietDvMoi.Sum(d => d.ThanhTien);
            decimal tongCongMoi = tienPhongMoi + tongDvMoi + noXuyen;

            var hdMoiHdId = await conn.ExecuteScalarAsync<int>("""
                INSERT INTO HoaDon
                    (HopDongId, Thang, Nam, NgayLap, TienPhong, TongTienDichVu,
                     TongCong, SoTienDaThu, TrangThaiThanhToan,
                     SoNgayO, SoNgayTrongThang, TienNoKyTruoc)
                VALUES
                    (@HopDongId, @Thang, @Nam, NOW(), @TienPhong, @TongDV,
                     @TongCong, 0, 'ChuaThu',
                     @SoNgayO, @SoNgayTrongThang, @NoXuyen);
                SELECT LAST_INSERT_ID();
                """,
                new
                {
                    HopDongId = hdMoiId,
                    Thang = thang,
                    Nam = nam,
                    TienPhong = tienPhongMoi,
                    TongDV = tongDvMoi,
                    TongCong = tongCongMoi,
                    NoXuyen = noXuyen,
                    SoNgayO = soNgayOMoi,
                    SoNgayTrongThang = soNgayTrongThang
                }, tx);

            await InsertChiTietAsync(conn, tx, hdMoiHdId, chiTietDvMoi);

            await conn.ExecuteAsync(
                "UPDATE HoaDon SET HoaDonGhepId=@Ghep WHERE Id=@Id",
                new { Ghep = hdMoiHdId, Id = hdCuId }, tx);

            await conn.ExecuteAsync(
                "UPDATE HoaDon SET HoaDonGhepId=@Ghep WHERE Id=@Id",
                new { Ghep = hdCuId, Id = hdMoiHdId }, tx);

            if (noXuyen > 0)
            {
                await congNoSettlementService.ThanhToanNoAsync(
                    conn,
                    tx,
                    vm.HopDongCuId,
                    noXuyen,
                    vm.NgayChuyenDi,
                    "KetChuyenNo",
                    $"Ket chuyen no sang hop dong #{hdMoiId}",
                    [hdCuId]);
            }

            await giaoDichCocService.ChuyenCocSangHopDongMoiAsync(
                conn,
                tx,
                hdCu,
                hdMoiId,
                vm.TienCocMoi,
                vm.NgayChuyenDi);

            await tx.CommitAsync();
            return (hdMoiId, hdCuId, hdMoiHdId);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private async Task<List<ChiTietDichVuTam>> TinhChiTietDichVuAsync(
        MySqlConnection conn,
        MySqlTransaction tx,
        int phongId,
        int hopDongId,
        IEnumerable<PhongDichVu> dichVuPhong,
        int thang,
        int nam)
    {
        var result = new List<ChiTietDichVuTam>();

        foreach (var dv in dichVuPhong)
        {
            var donGia = await LayGiaDichVuAsync(dv, thang, nam);

            if (dv.DichVu?.LoaiTinhPhi == "TheoChiSo")
            {
                var chiSo = await LayChiSoAsync(conn, tx, phongId, hopDongId, dv.DichVuId, thang, nam);
                if (chiSo == null)
                    throw new InvalidOperationException($"Thieu chi so {dv.DichVu.TenDichVu} ky {thang}/{nam} de chuyen phong.");

                var soLuong = ChiSoConsumptionCalculator.Calculate(chiSo);
                result.Add(new ChiTietDichVuTam(
                    dv.DichVuId,
                    chiSo.Id,
                    soLuong,
                    donGia,
                    Math.Round(soLuong * donGia, 0)));
            }
            else
            {
                result.Add(new ChiTietDichVuTam(
                    dv.DichVuId,
                    null,
                    1,
                    donGia,
                    Math.Round(donGia, 0)));
            }
        }

        return result;
    }

    private static async Task InsertChiTietAsync(
        MySqlConnection conn,
        MySqlTransaction tx,
        int hoaDonId,
        IEnumerable<ChiTietDichVuTam> chiTiet)
    {
        const string sql = """
            INSERT INTO ChiTietHoaDon
                (HoaDonId, DichVuId, ChiSoDienNuocId, SoLuong, DonGia, ThanhTien)
            VALUES
                (@HoaDonId, @DichVuId, @ChiSoDienNuocId, @SoLuong, @DonGia, @ThanhTien)
            """;

        foreach (var ct in chiTiet)
        {
            await conn.ExecuteAsync(sql, new
            {
                HoaDonId = hoaDonId,
                ct.DichVuId,
                ct.ChiSoDienNuocId,
                ct.SoLuong,
                ct.DonGia,
                ct.ThanhTien
            }, tx);
        }
    }

    private async Task<decimal> LayGiaPhongAsync(HopDong hd, int thang, int nam)
    {
        var ls = await lichSuRepo.GetGiaApDungAsync("Phong", hd.PhongId, thang, nam);
        return ls?.GiaMoi ?? hd.TienThueThoaThuan;
    }

    private async Task<decimal> LayGiaDichVuAsync(PhongDichVu dv, int thang, int nam)
    {
        var ls = await lichSuRepo.GetGiaApDungAsync("DichVu", dv.Id, thang, nam);
        return ls?.GiaMoi ?? dv.DonGia;
    }

    private static async Task<ChiSoDienNuoc?> LayChiSoAsync(
        MySqlConnection conn,
        MySqlTransaction tx,
        int phongId,
        int hopDongId,
        int dichVuId,
        int thang,
        int nam)
    {
        const string sql = """
            SELECT * FROM ChiSoDienNuoc
            WHERE PhongId = @PhongId
              AND (HopDongId = @HopDongId OR HopDongId IS NULL)
              AND DichVuId = @DichVuId
              AND Thang = @Thang
              AND Nam = @Nam
            ORDER BY CASE WHEN HopDongId = @HopDongId THEN 0 ELSE 1 END, Id DESC
            LIMIT 1
            """;

        return await conn.QueryFirstOrDefaultAsync<ChiSoDienNuoc>(
            sql,
            new { PhongId = phongId, HopDongId = hopDongId, DichVuId = dichVuId, Thang = thang, Nam = nam },
            tx);
    }

    private sealed record ChiTietDichVuTam(
        int DichVuId,
        int? ChiSoDienNuocId,
        decimal SoLuong,
        decimal DonGia,
        decimal ThanhTien);
}
