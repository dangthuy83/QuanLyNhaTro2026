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
    HopDongDichVuRepository hopDongDichVuRepo,
    LichSuThayDoiGiaRepository lichSuRepo,
    KhoanPhatSinhHopDongRepository khoanPhatSinhRepo,
    GiaoDichCocService giaoDichCocService,
    CongNoSettlementService congNoSettlementService,
    HoaDonSnapshotService snapshotService,
    PhongLifecycleService phongLifecycle)
{
    public async Task<(int HopDongMoiId, int HoaDonCuId, int? HoaDonMoiId)> ThucHienAsync(
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
        bool chuyenCuoiThang = vm.NgayChuyenDi.Day == soNgayTrongThang;

        if (soNgayOCu <= 0)
            throw new InvalidOperationException("Hop dong cu khong co ngay o trong ky chuyen phong.");

        if (soNgayOMoi <= 0 && !chuyenCuoiThang)
            throw new InvalidOperationException("Hop dong moi khong co ngay o hop le trong ky chuyen phong.");

        decimal giaPhongCu = await LayGiaPhongAsync(hdCu, thang, nam);
        decimal tienPhongCu = BillingPeriodCalculator.CalculateRoomCharge(giaPhongCu, soNgayOCu, soNgayTrongThang);
        decimal tienPhongMoi = chuyenCuoiThang
            ? 0
            : BillingPeriodCalculator.CalculateRoomCharge(vm.TienThueMoi, soNgayOMoi, soNgayTrongThang);

        var dvCu = (await hopDongDichVuRepo.GetPhongDichVuByHopDongKyAsync(
            vm.HopDongCuId, thang, nam)).ToList();

        decimal noXuyen = await hoaDonRepo.GetTongNoConLaiAsync(vm.HopDongCuId);

        var conn = new MySqlConnection(config.GetConnectionString("DefaultConnection"));
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();

        try
        {
            var phongIds = new[] { hdCu.PhongId, vm.PhongMoiId }.Distinct().OrderBy(x => x).ToArray();
            foreach (var phongId in phongIds)
            {
                await phongLifecycle.KhoaPhongAsync(conn, tx, phongId);
            }

            var hdCuDaKhoa = await conn.QueryFirstOrDefaultAsync<HopDong>(
                "SELECT * FROM HopDong WHERE Id = @Id FOR UPDATE",
                new { Id = vm.HopDongCuId }, tx)
                ?? throw new InvalidOperationException("Khong tim thay hop dong.");
            if (hdCuDaKhoa.TrangThai != "DangHieuLuc")
                throw new InvalidOperationException("Hop dong khong con hieu luc de chuyen phong.");
            if (hdCuDaKhoa.PhongId != hdCu.PhongId)
                throw new InvalidOperationException("Phong cua hop dong da thay doi. Vui long tai lai du lieu.");

            var phongMoiDaKhoa = await phongLifecycle.KhoaPhongAsync(conn, tx, vm.PhongMoiId);
            PhongLifecycleService.DamBaoKhongDangSua(phongMoiDaKhoa);
            if (await hopDongRepo.CoChongKhoangAsync(
                    conn, tx, vm.PhongMoiId, vm.NgayBatDauMoi, null))
                throw new InvalidOperationException("Phong moi da co hop dong chiem dung trong khoang thoi gian nay.");

            await conn.ExecuteAsync(
                "UPDATE HopDong SET TrangThai='DaChuyenPhong', NgayKetThuc=@Ngay WHERE Id=@Id",
                new { Ngay = vm.NgayChuyenDi, Id = vm.HopDongCuId }, tx);

            await PhongLifecycleService.DongBoTrangThaiTheoNgayAsync(
                conn, tx, hdCuDaKhoa.PhongId, DateTime.Today);

            var trangThaiHopDongMoi = vm.NgayBatDauMoi.Date > DateTime.Today
                ? "ChoHieuLuc"
                : "DangHieuLuc";
            var hdMoiId = await conn.ExecuteScalarAsync<int>("""
                INSERT INTO HopDong
                    (PhongId, NgayBatDau, TienThueThoaThuan, TienCoc,
                     NgayThanhToanHangThang, TrangThai, HopDongTruocId,
                     DaXuLyChenhLechCoc, GhiChu, NgayTao)
                VALUES
                    (@PhongId, @NgayBatDau, @TienThue, @TienCoc,
                     @NgayThanhToanHangThang, @TrangThai, @HopDongTruocId,
                     @CanXuLyCoc, @GhiChu, NOW());
                SELECT LAST_INSERT_ID();
                """,
                new
                {
                    PhongId = vm.PhongMoiId,
                    NgayBatDau = vm.NgayBatDauMoi,
                    TienThue = vm.TienThueMoi,
                    TienCoc = vm.TienCocMoi,
                    NgayThanhToanHangThang = hdCuDaKhoa.NgayThanhToanHangThang,
                    TrangThai = trangThaiHopDongMoi,
                    HopDongTruocId = vm.HopDongCuId,
                    CanXuLyCoc = vm.TienCocMoi != hdCu.TienCoc,
                    GhiChu = $"Chuyen tu {phongCu.TenPhong} ngay {vm.NgayChuyenDi:dd/MM/yyyy}"
                }, tx);

            await CuTruService.ChuyenSangHopDongMoiAsync(
                conn, tx, vm.HopDongCuId, hdMoiId, vm.NgayChuyenDi, vm.NgayBatDauMoi);

            var selectedIds = vm.PhongDichVuIds.Distinct().ToHashSet();
            var dvMoi = await phongDvRepo.GetSelectedForPhongAsync(
                conn, tx, vm.PhongMoiId, selectedIds);
            if (dvMoi.Count != selectedIds.Count)
                throw new InvalidOperationException("Danh sach dich vu phong moi khong hop le.");
            var requiredIds = await phongDvRepo.GetRequiredIdsForPhongAsync(conn, tx, vm.PhongMoiId);
            if (requiredIds.Except(selectedIds).Any())
                throw new InvalidOperationException("Phai chon day du cac dich vu bat buoc cua phong moi.");
            await hopDongDichVuRepo.InsertManyAsync(
                conn, tx, hdMoiId, selectedIds, vm.NgayBatDauMoi);

            await PhongLifecycleService.DongBoTrangThaiTheoNgayAsync(
                conn, tx, vm.PhongMoiId, DateTime.Today);

            var dichVuTinhChoPhongCu = chuyenCuoiThang
                ? dvCu
                : dvCu.Where(d => d.DichVu?.LoaiTinhPhi == "TheoChiSo");
            var chiTietDvCu = await TinhChiTietDichVuAsync(
                conn, tx, hdCu.PhongId, vm.HopDongCuId, dichVuTinhChoPhongCu, thang, nam);
            decimal tongDvCu = chiTietDvCu.Sum(d => d.ThanhTien);
            var khoanPhatSinhCu = await khoanPhatSinhRepo.GetChuaXuLyDenNgayAsync(
                conn, tx, vm.HopDongCuId, vm.NgayChuyenDi);
            decimal tongPhatSinhCu = khoanPhatSinhCu.Sum(x => x.SoTienConLai);
            decimal tongCongCu = tienPhongCu + tongDvCu + tongPhatSinhCu;

            var hdCuId = await snapshotService.InsertHoaDonAsync(conn, tx, new HoaDon
                {
                    HopDongId = vm.HopDongCuId,
                    Thang = thang,
                    Nam = nam,
                    NgayLap = DateTime.Now,
                    TienPhong = tienPhongCu,
                    TongTienDichVu = tongDvCu,
                    TongTienPhatSinh = tongPhatSinhCu,
                    TongCong = tongCongCu,
                    SoNgayO = soNgayOCu,
                    SoNgayTrongThang = soNgayTrongThang,
                    TrangThaiThanhToan = "ChuaThu"
                });

            await InsertChiTietAsync(conn, tx, hdCuId, chiTietDvCu);
            await khoanPhatSinhRepo.GanVaoHoaDonAsync(
                conn, tx, khoanPhatSinhCu.Select(x => x.Id), hdCuId);

            int? hdMoiHdId = null;
            if (!chuyenCuoiThang)
            {
                var chiTietDvMoi = await TinhChiTietDichVuAsync(conn, tx, vm.PhongMoiId, hdMoiId, dvMoi, thang, nam);
                decimal tongDvMoi = chiTietDvMoi.Sum(d => d.ThanhTien);
                decimal tongCongMoi = tienPhongMoi + tongDvMoi + noXuyen;

                hdMoiHdId = await snapshotService.InsertHoaDonAsync(conn, tx, new HoaDon
                    {
                        HopDongId = hdMoiId,
                        Thang = thang,
                        Nam = nam,
                        NgayLap = DateTime.Now,
                        TienPhong = tienPhongMoi,
                        TongTienDichVu = tongDvMoi,
                        TongCong = tongCongMoi,
                        TienNoKyTruoc = noXuyen,
                        SoNgayO = soNgayOMoi,
                        SoNgayTrongThang = soNgayTrongThang,
                        TrangThaiThanhToan = "ChuaThu"
                    });

                await InsertChiTietAsync(conn, tx, hdMoiHdId.Value, chiTietDvMoi);

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
                var kyBatDau = new DateTime(nam, thang, 1);
                var kyKetThuc = kyBatDau.AddMonths(1).AddDays(-1);
                var soLuong = await FixedServiceQuantityCalculator.ResolveQuantityAsync(
                    conn, tx, hopDongId, dv.DichVu!, kyBatDau, kyKetThuc);
                result.Add(new ChiTietDichVuTam(
                    dv.DichVuId,
                    null,
                    soLuong,
                    donGia,
                    Math.Round(soLuong * donGia, 0)));
            }
        }

        return result;
    }

    private async Task InsertChiTietAsync(
        MySqlConnection conn,
        MySqlTransaction tx,
        int hoaDonId,
        IEnumerable<ChiTietDichVuTam> chiTiet)
    {
        await snapshotService.InsertChiTietAsync(
            conn,
            tx,
            hoaDonId,
            chiTiet.Select(ct => new ChiTietHoaDon
            {
                DichVuId = ct.DichVuId,
                ChiSoDienNuocId = ct.ChiSoDienNuocId,
                SoLuong = ct.SoLuong,
                DonGia = ct.DonGia,
                ThanhTien = ct.ThanhTien
            }));
    }

    private async Task<decimal> LayGiaPhongAsync(HopDong hd, int thang, int nam)
    {
        var gia = await lichSuRepo.GetGiaTriApDungAsync("HopDong", hd.Id, thang, nam);
        return gia ?? hd.TienThueThoaThuan;
    }

    private async Task<decimal> LayGiaDichVuAsync(PhongDichVu dv, int thang, int nam)
    {
        var gia = await lichSuRepo.GetGiaTriApDungAsync("DichVu", dv.Id, thang, nam);
        return gia ?? dv.DonGia;
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
