using Dapper;
using MySqlConnector;
using QuanLyNhaTro.Models;
using QuanLyNhaTro.Repositories;

namespace QuanLyNhaTro.Services;

public class TraPhongService(
    IConfiguration config,
    HopDongRepository hopDongRepo,
    HoaDonRepository hoaDonRepo,
    PhongRepository phongRepo,
    HopDongDichVuRepository hopDongDichVuRepo,
    KhachThueRepository khachRepo,
    LichSuThayDoiGiaRepository lichSuRepo,
    KhoanPhatSinhHopDongRepository khoanPhatSinhRepo,
    GiaoDichCocService giaoDichCocService,
    CongNoSettlementService congNoSettlementService,
    HoaDonSnapshotService snapshotService,
    PhongLifecycleService phongLifecycle)
{
    public async Task<TraPhongViewModel> TinhPreviewAsync(int hopDongId, DateTime ngayTraPhong)
    {
        var hd = await hopDongRepo.GetByIdAsync(hopDongId)
            ?? throw new InvalidOperationException("Khong tim thay hop dong.");

        if (ngayTraPhong.Date < hd.NgayBatDau.Date)
            throw new InvalidOperationException("Ngay tra phong khong duoc truoc ngay bat dau hop dong.");

        var phong = await phongRepo.GetByIdAsync(hd.PhongId);
        var khach = (await khachRepo.GetByHopDongAsync(hopDongId)).FirstOrDefault();
        var dvPhong = (await hopDongDichVuRepo.GetPhongDichVuByHopDongKyAsync(
            hopDongId, ngayTraPhong.Month, ngayTraPhong.Year)).ToList();

        int thang = ngayTraPhong.Month;
        int nam = ngayTraPhong.Year;
        int soNgayTrongThang = BillingPeriodCalculator.GetDaysInMonth(thang, nam);
        int soNgayO = BillingPeriodCalculator.CountOccupiedDays(thang, nam, hd.NgayBatDau, ngayTraPhong);

        if (soNgayO <= 0)
            throw new InvalidOperationException("Hop dong khong co ngay o trong ky tra phong.");

        var hdThangNay = await hoaDonRepo.GetByHopDongKyLoaiAsync(
            hopDongId, thang, nam, "DinhKy");
        var quyetToanDaCo = await hoaDonRepo.GetByHopDongKyLoaiAsync(
            hopDongId, thang, nam, "QuyetToanTraPhong");
        bool canSinhHd = quyetToanDaCo == null;
        string? lyDoChanTraPhong = null;

        if (quyetToanDaCo != null)
            lyDoChanTraPhong = $"Hợp đồng đã có hóa đơn quyết toán trả phòng #{quyetToanDaCo.Id}.";
        if (hdThangNay != null && hdThangNay.SoTienDaThu < hdThangNay.TienPhong)
            lyDoChanTraPhong =
                $"Hóa đơn kỳ thu #{hdThangNay.Id} chưa ghi nhận đủ phần tiền phòng trả trước.";

        decimal giaPhong = await LayGiaPhongAsync(hd, thang, nam);
        decimal tienPhongProRata = BillingPeriodCalculator.CalculateRoomCharge(
            giaPhong, soNgayO, soNgayTrongThang);
        decimal tienPhongDaTraTruoc = hdThangNay == null
            ? 0
            : Math.Min(hdThangNay.TienPhong, hdThangNay.SoTienDaThu);
        decimal tinDungTaoMoi = Math.Max(0, tienPhongDaTraTruoc - tienPhongProRata);
        decimal tienPhongPhaiLap = hdThangNay == null
            ? tienPhongProRata
            : Math.Max(0, tienPhongProRata - tienPhongDaTraTruoc);
        decimal tongDichVuThangCuoi = 0;

        if (canSinhHd && lyDoChanTraPhong == null)
        {
            await using var conn = new MySqlConnection(config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();
            try
            {
                var chiTietDv = await TinhChiTietDichVuAsync(
                    conn, null, hd.PhongId, hopDongId, dvPhong, thang, nam);
                tongDichVuThangCuoi = chiTietDv.Sum(d => d.ThanhTien);
            }
            catch (ThieuChiSoTraPhongException ex)
            {
                lyDoChanTraPhong = ex.Message;
            }
        }

        decimal tongNo = await hoaDonRepo.GetTongNoConLaiAsync(hopDongId);
        var khoanPhatSinh = await khoanPhatSinhRepo.GetChuaXuLyDenNgayAsync(hopDongId, ngayTraPhong);
        decimal tongPhatSinhChuaXuLy = khoanPhatSinh.Sum(x => x.SoTienConLai);
        if (canSinhHd && lyDoChanTraPhong == null)
            tongNo += tienPhongPhaiLap + tongDichVuThangCuoi + tongPhatSinhChuaXuLy;
        else tongNo += tongPhatSinhChuaXuLy;
        decimal tinDungApDung = Math.Min(tinDungTaoMoi, Math.Max(0, tongNo));
        tongNo -= tinDungApDung;
        decimal tienHoanTinDung = tinDungTaoMoi - tinDungApDung;
        decimal soDuCoc = await giaoDichCocService.GetSoDuHienTaiAsync(hopDongId);
        decimal tienTruNoTuCoc = Math.Min(soDuCoc, Math.Max(0, tongNo));

        return new TraPhongViewModel
        {
            HopDongId = hopDongId,
            TenPhong = phong?.TenPhong ?? $"Phong #{hd.PhongId}",
            TenKhachChinh = khach?.HoTen ?? "(chua co khach)",
            TienCoc = soDuCoc,
            NgayTraPhong = ngayTraPhong,
            CoTheTraPhong = lyDoChanTraPhong == null,
            LyDoChanTraPhong = lyDoChanTraPhong,
            HoaDonKyTraPhongId = hdThangNay?.Id,
            CanSinhHoaDonMoi = canSinhHd,
            SoNgayO = soNgayO,
            SoNgayTrongThang = soNgayTrongThang,
            TienPhongProRata = tienPhongProRata,
            TienPhongDaTraTruoc = tienPhongDaTraTruoc,
            TinDungTienPhongTaoMoi = tinDungTaoMoi,
            TinDungApDung = tinDungApDung,
            TienHoanTinDung = tienHoanTinDung,
            TongTienDichVuThangCuoi = tongDichVuThangCuoi,
            TongTienPhatSinhChuaXuLy = tongPhatSinhChuaXuLy,
            TongNoConLai = tongNo,
            TienTruNoTuCoc = tienTruNoTuCoc,
            TienHoanCoc = Math.Max(0, soDuCoc - tongNo),
            KhachConNoThem = Math.Max(0, tongNo - soDuCoc)
        };
    }

    public async Task<KetQuaTraPhongViewModel> ThucHienAsync(
        int hopDongId,
        DateTime ngayTraPhong,
        string? ghiChu)
    {
        var hd = await hopDongRepo.GetByIdAsync(hopDongId)
            ?? throw new InvalidOperationException("Khong tim thay hop dong.");

        if (ngayTraPhong.Date < hd.NgayBatDau.Date)
            throw new InvalidOperationException("Ngay tra phong khong duoc truoc ngay bat dau hop dong.");

        var phong = await phongRepo.GetByIdAsync(hd.PhongId);
        var khach = (await khachRepo.GetByHopDongAsync(hopDongId)).FirstOrDefault();
        var dvPhong = (await hopDongDichVuRepo.GetPhongDichVuByHopDongKyAsync(
            hopDongId, ngayTraPhong.Month, ngayTraPhong.Year)).ToList();

        int thang = ngayTraPhong.Month;
        int nam = ngayTraPhong.Year;

        await using var conn = new MySqlConnection(config.GetConnectionString("DefaultConnection"));
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();

        try
        {
            await phongLifecycle.KhoaPhongAsync(conn, tx, hd.PhongId);
            var hdDaKhoa = await conn.QueryFirstOrDefaultAsync<HopDong>(
                "SELECT * FROM HopDong WHERE Id = @Id FOR UPDATE",
                new { Id = hopDongId },
                tx) ?? throw new InvalidOperationException("Khong tim thay hop dong.");
            if (hdDaKhoa.TrangThai != "DangHieuLuc")
                throw new InvalidOperationException("Hop dong khong con hieu luc de tra phong.");

            if (ngayTraPhong.Date < hdDaKhoa.NgayBatDau.Date)
                throw new InvalidOperationException("Ngay tra phong khong duoc truoc ngay bat dau hop dong.");

            int soNgayTrongThang = BillingPeriodCalculator.GetDaysInMonth(thang, nam);
            int soNgayO = BillingPeriodCalculator.CountOccupiedDays(
                thang, nam, hdDaKhoa.NgayBatDau, ngayTraPhong);
            if (soNgayO <= 0)
                throw new InvalidOperationException("Hop dong khong co ngay o trong ky tra phong.");

            var hdThangNay = await hoaDonRepo.GetByHopDongKyForUpdateAsync(
                conn, tx, hopDongId, thang, nam);
            if (await hoaDonRepo.GetByHopDongKyLoaiForUpdateAsync(
                    conn, tx, hopDongId, thang, nam, "QuyetToanTraPhong") != null)
                throw new InvalidOperationException("Hợp đồng đã có hóa đơn quyết toán trả phòng.");
            if (hdThangNay != null && hdThangNay.SoTienDaThu < hdThangNay.TienPhong)
                throw new InvalidOperationException(
                    $"Hóa đơn kỳ thu #{hdThangNay.Id} chưa ghi nhận đủ phần tiền phòng trả trước.");

            decimal giaPhong = await LayGiaPhongAsync(hdDaKhoa, thang, nam);
            decimal tienPhongThucTe = BillingPeriodCalculator.CalculateRoomCharge(
                giaPhong, soNgayO, soNgayTrongThang);
            decimal tienPhongDaTraTruoc = hdThangNay == null
                ? 0
                : Math.Min(hdThangNay.TienPhong, hdThangNay.SoTienDaThu);
            decimal tinDungTaoMoi = Math.Max(0, tienPhongDaTraTruoc - tienPhongThucTe);
            decimal tienPhongPhaiLap = hdThangNay == null
                ? tienPhongThucTe
                : Math.Max(0, tienPhongThucTe - tienPhongDaTraTruoc);
            await TinDungTienPhongService.TaoKhiTraPhongAsync(
                conn, tx, hopDongId, tinDungTaoMoi, ngayTraPhong, "Hệ thống");

            int? hoaDonCuoiId = null;
            List<KhoanPhatSinhHopDong> khoanPhatSinhChuaXuLy = [];
            decimal noKyTruoc = await TinhTongNoConLaiAsync(conn, tx, hopDongId);
            var chiTietDv = await TinhChiTietDichVuAsync(
                conn, tx, hdDaKhoa.PhongId, hopDongId, dvPhong, thang, nam);
            decimal tongDv = chiTietDv.Sum(d => d.ThanhTien);
            khoanPhatSinhChuaXuLy = await khoanPhatSinhRepo.GetChuaXuLyDenNgayAsync(
                conn, tx, hopDongId, ngayTraPhong);
            decimal tongPhatSinh = khoanPhatSinhChuaXuLy.Sum(x => x.SoTienConLai);
            decimal truocTinDung = tienPhongPhaiLap + tongDv + tongPhatSinh + noKyTruoc;
            decimal soDuTinDung = await TinDungTienPhongService.GetSoDuForUpdateAsync(
                conn, tx, hopDongId);
            decimal tinDungApDung = Math.Min(soDuTinDung, truocTinDung);
            decimal tongCong = truocTinDung - tinDungApDung;
            var settlementPeriods = BillingCollectionPeriodPolicy.ResolveSettlement(ngayTraPhong);

            hoaDonCuoiId = await snapshotService.InsertHoaDonAsync(
                conn,
                tx,
                new HoaDon
                {
                    HopDongId = hopDongId,
                    KyThu = settlementPeriods.KyThu,
                    KyTienPhong = settlementPeriods.KyTienPhong,
                    KyDichVu = settlementPeriods.KyDichVu,
                    LoaiHoaDon = "QuyetToanTraPhong",
                    Thang = thang,
                    Nam = nam,
                    NgayLap = DateTime.Now,
                    NgayDenHan = settlementPeriods.NgayDenHan,
                    TienPhong = tienPhongPhaiLap,
                    TongTienDichVu = tongDv,
                    TongTienPhatSinh = tongPhatSinh,
                    TongCong = tongCong,
                    SoNgayO = soNgayO == soNgayTrongThang ? null : soNgayO,
                    SoNgayTrongThang = soNgayO == soNgayTrongThang ? null : soNgayTrongThang,
                    TienNoKyTruoc = noKyTruoc,
                    TienTinDungApDung = tinDungApDung,
                    TrangThaiThanhToan = tongCong == 0 ? "DaThu" : "ChuaThu",
                    GhiChu = $"Trả phòng ngày {ngayTraPhong:dd/MM/yyyy}; tiền phòng thực tế "
                        + $"{tienPhongThucTe:N0} đ, đã trả trước {tienPhongDaTraTruoc:N0} đ. {ghiChu}".Trim()
                });

            await InsertChiTietAsync(conn, tx, hoaDonCuoiId.Value, chiTietDv, thang, nam);
            await khoanPhatSinhRepo.GanVaoHoaDonAsync(
                conn,
                tx,
                khoanPhatSinhChuaXuLy.Select(x => x.Id),
                hoaDonCuoiId.Value);
            var tinDungDaApDung = await TinDungTienPhongService.ApDungVaoHoaDonAsync(
                conn, tx, hopDongId, hoaDonCuoiId.Value, truocTinDung, ngayTraPhong, "Hệ thống");
            if (tinDungDaApDung != tinDungApDung)
                throw new InvalidOperationException("Số dư tín dụng thay đổi trong lúc quyết toán trả phòng.");

            if (noKyTruoc > 0)
            {
                var daKetChuyen = await congNoSettlementService.ThanhToanNoAsync(
                    conn, tx, hopDongId, noKyTruoc, ngayTraPhong, "KetChuyenNo",
                    $"Kết chuyển nợ sang hóa đơn trả phòng #{hoaDonCuoiId.Value}",
                    [hoaDonCuoiId.Value]);
                if (daKetChuyen != noKyTruoc)
                    throw new InvalidOperationException("Số tiền nợ cũ không khớp khi quyết toán trả phòng.");
            }

            decimal tienHoanTinDung = await TinDungTienPhongService.HoanTienAsync(
                conn, tx, hopDongId, ngayTraPhong, "Hệ thống",
                $"HOAN_TIN_DUNG_TRA_PHONG:{hopDongId}:{ngayTraPhong:yyyyMMdd}");

            await CuTruService.DongTatCaDangMoAsync(conn, tx, hopDongId, ngayTraPhong);

            await conn.ExecuteAsync(
                """
                UPDATE HopDong
                SET TrangThai = 'DaKetThuc',
                    NgayKetThuc = @Ngay,
                    NgayTraPhongThucTe = @Ngay,
                    GhiChu = CONCAT(COALESCE(GhiChu, ''), @GhiChu)
                WHERE Id = @Id
                """,
                new
                {
                    Ngay = ngayTraPhong,
                    GhiChu = $" [Tra phong {ngayTraPhong:dd/MM/yyyy}]",
                    Id = hopDongId
                },
                tx);

            await PhongLifecycleService.DongBoTrangThaiTheoNgayAsync(
                conn, tx, hdDaKhoa.PhongId, DateTime.Today);

            decimal tongNoHoaDonTruocXuLyCoc = await TinhTongNoConLaiAsync(conn, tx, hopDongId);
            decimal tongPhatSinhChuaXuLy = 0;
            decimal tongNoTruocXuLyCoc = tongNoHoaDonTruocXuLyCoc + tongPhatSinhChuaXuLy;
            var ketQuaCoc = await giaoDichCocService.TatToanCocKhiTraPhongAsync(
                conn,
                tx,
                hdDaKhoa,
                hoaDonCuoiId,
                tongNoTruocXuLyCoc,
                ngayTraPhong,
                ghiChu);

            if (ketQuaCoc.SoTienTruNo > 0)
            {
                var soTienTruNoHoaDon = Math.Min(ketQuaCoc.SoTienTruNo, tongNoHoaDonTruocXuLyCoc);
                if (soTienTruNoHoaDon > 0)
                {
                    await congNoSettlementService.ThanhToanNoAsync(
                        conn,
                        tx,
                        hopDongId,
                        soTienTruNoHoaDon,
                        ngayTraPhong,
                        "TruCoc",
                        $"Tru no vao coc khi tra phong hop dong #{hopDongId}");
                }

                var soTienTruPhatSinh = ketQuaCoc.SoTienTruNo - soTienTruNoHoaDon;
                if (soTienTruPhatSinh > 0)
                {
                    var daTruPhatSinh = await khoanPhatSinhRepo.ApDungTruCocAsync(
                        conn,
                        tx,
                        hopDongId,
                        soTienTruPhatSinh,
                        ngayTraPhong);

                    if (daTruPhatSinh != soTienTruPhatSinh)
                        throw new InvalidOperationException("So tien tru coc cho khoan phat sinh khong khop.");
                }
            }

            decimal tongNoHoaDonConLai = await TinhTongNoConLaiAsync(conn, tx, hopDongId);
            decimal tongPhatSinhConLai = (await khoanPhatSinhRepo.GetChuaXuLyDenNgayAsync(conn, tx, hopDongId, ngayTraPhong))
                .Sum(x => x.SoTienConLai);
            decimal tongNoConLai = tongNoHoaDonConLai + tongPhatSinhConLai;

            await conn.ExecuteAsync(
                "UPDATE HopDong SET TienCocHoanLai = @TienCocHoanLai WHERE Id = @Id",
                new { Id = hopDongId, TienCocHoanLai = ketQuaCoc.SoTienHoanCoc },
                tx);

            await tx.CommitAsync();

            return new KetQuaTraPhongViewModel
            {
                PhongId = hdDaKhoa.PhongId,
                TenPhong = phong?.TenPhong ?? $"Phong #{hdDaKhoa.PhongId}",
                TenKhachChinh = khach?.HoTen ?? "",
                NgayTraPhong = ngayTraPhong,
                HoaDonCuoiId = hoaDonCuoiId,
                TienCoc = ketQuaCoc.SoDuCocTruocXuLy,
                TongNoConLai = tongNoConLai,
                TongTienPhatSinhConLai = tongPhatSinhConLai,
                TienTruNoTuCoc = ketQuaCoc.SoTienTruNo,
                TinDungTienPhongTaoMoi = tinDungTaoMoi,
                TinDungApDung = tinDungApDung,
                TienHoanTinDung = tienHoanTinDung,
                TienHoanCoc = ketQuaCoc.SoTienHoanCoc,
                KhachConNoThem = ketQuaCoc.KhachConNoThem,
                CoNoTon = tongNoConLai > 0
            };
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private async Task<List<ChiTietDichVuTam>> TinhChiTietDichVuAsync(
        MySqlConnection conn,
        MySqlTransaction? tx,
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
                    throw new ThieuChiSoTraPhongException(
                        $"Thieu chi so {dv.DichVu.TenDichVu} ky {thang}/{nam} de tra phong.");

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
        IEnumerable<ChiTietDichVuTam> chiTiet,
        int thang,
        int nam)
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
                ThanhTien = ct.ThanhTien,
                KySuDung = new DateTime(nam, thang, 1)
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
        MySqlTransaction? tx,
        int phongId,
        int hopDongId,
        int dichVuId,
        int thang,
        int nam)
    {
        const string sql = """
            SELECT *
            FROM ChiSoDienNuoc
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

    private static async Task<decimal> TinhTongNoConLaiAsync(
        MySqlConnection conn,
        MySqlTransaction tx,
        int hopDongId)
        => await conn.ExecuteScalarAsync<decimal>(
            """
            SELECT COALESCE(SUM(TongCong - SoTienDaThu), 0)
            FROM HoaDon
            WHERE HopDongId = @HopDongId
              AND TongCong > SoTienDaThu
            """,
            new { HopDongId = hopDongId },
            tx);

    private static KetQuaDanhGiaHoaDonKyTraPhong DanhGiaHoaDonKyTraPhong(
        HoaDon? hoaDon,
        int soNgayO,
        int soNgayTrongThang)
    {
        if (hoaDon == null)
            return KetQuaDanhGiaHoaDonKyTraPhong.TaoMoi;

        bool kyTraPhongDuThang = soNgayO == soNgayTrongThang;
        bool hoaDonNullNull = hoaDon.SoNgayO == null && hoaDon.SoNgayTrongThang == null;
        bool hoaDonDuThangRoRang = hoaDon.SoNgayO == soNgayTrongThang
            && hoaDon.SoNgayTrongThang == soNgayTrongThang;

        if (kyTraPhongDuThang)
        {
            return hoaDonNullNull || hoaDonDuThangRoRang
                ? KetQuaDanhGiaHoaDonKyTraPhong.SuDungHienCo
                : KetQuaDanhGiaHoaDonKyTraPhong.BiChan;
        }

        return hoaDon.SoNgayO == soNgayO && hoaDon.SoNgayTrongThang == soNgayTrongThang
            ? KetQuaDanhGiaHoaDonKyTraPhong.SuDungHienCo
            : KetQuaDanhGiaHoaDonKyTraPhong.BiChan;
    }

    private static string TaoThongBaoHoaDonKhongKhop(
        HoaDon hoaDon,
        HoaDonDeletionAssessment deletion)
    {
        if (deletion.CanDelete)
        {
            return $"Hóa đơn #{hoaDon.Id} của kỳ trả phòng không khớp số ngày ở. "
                + "Hãy xóa hóa đơn này, lập lại đúng kỳ/ngày ở, rồi thực hiện trả phòng.";
        }

        return $"Hóa đơn #{hoaDon.Id} của kỳ trả phòng không khớp số ngày ở và không thể xóa vì "
            + $"{deletion.BlockReason}. Phase hiện tại không dùng credit note và không tự điều chỉnh; "
            + "không thể tiếp tục trả phòng.";
    }

    private enum KetQuaDanhGiaHoaDonKyTraPhong
    {
        TaoMoi,
        SuDungHienCo,
        BiChan
    }

    private static async Task<decimal> TinhNoKyTruocAsync(
        MySqlConnection conn,
        MySqlTransaction tx,
        int hopDongId,
        int thang,
        int nam)
    {
        var noTruocKy = await conn.ExecuteScalarAsync<decimal>(
            """
            SELECT COALESCE(SUM(TongCong - SoTienDaThu), 0)
            FROM HoaDon
            WHERE HopDongId = @HopDongId
              AND TongCong > SoTienDaThu
              AND (Nam < @Nam OR (Nam = @Nam AND Thang < @Thang))
            """,
            new { HopDongId = hopDongId, Thang = thang, Nam = nam },
            tx);

        if (noTruocKy > 0)
            return noTruocKy;

        return await conn.ExecuteScalarAsync<decimal>(
            """
            SELECT COALESCE((
                SELECT TongCong - SoTienDaThu
                FROM HoaDon
                WHERE HopDongId = @HopDongId
                  AND (Nam < @Nam OR (Nam = @Nam AND Thang < @Thang))
                ORDER BY Nam DESC, Thang DESC
                LIMIT 1
            ), 0)
            """,
            new { HopDongId = hopDongId, Thang = thang, Nam = nam },
            tx);
    }

    private sealed record ChiTietDichVuTam(
        int DichVuId,
        int? ChiSoDienNuocId,
        decimal SoLuong,
        decimal DonGia,
        decimal ThanhTien);

    private sealed class ThieuChiSoTraPhongException(string message)
        : InvalidOperationException(message);
}
