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
    PhongDichVuRepository phongDvRepo,
    KhachThueRepository khachRepo,
    LichSuThayDoiGiaRepository lichSuRepo,
    GiaoDichCocService giaoDichCocService,
    CongNoSettlementService congNoSettlementService)
{
    public async Task<TraPhongViewModel> TinhPreviewAsync(int hopDongId, DateTime ngayTraPhong)
    {
        var hd = await hopDongRepo.GetByIdAsync(hopDongId)
            ?? throw new InvalidOperationException("Khong tim thay hop dong.");

        if (ngayTraPhong.Date < hd.NgayBatDau.Date)
            throw new InvalidOperationException("Ngay tra phong khong duoc truoc ngay bat dau hop dong.");

        var phong = await phongRepo.GetByIdAsync(hd.PhongId);
        var khach = (await khachRepo.GetByHopDongAsync(hopDongId)).FirstOrDefault();
        var dvPhong = (await phongDvRepo.GetByPhongAsync(hd.PhongId)).ToList();

        int thang = ngayTraPhong.Month;
        int nam = ngayTraPhong.Year;
        int soNgayTrongThang = BillingPeriodCalculator.GetDaysInMonth(thang, nam);
        int soNgayO = BillingPeriodCalculator.CountOccupiedDays(thang, nam, hd.NgayBatDau, ngayTraPhong);

        if (soNgayO <= 0)
            throw new InvalidOperationException("Hop dong khong co ngay o trong ky tra phong.");

        var hdThangNay = await hoaDonRepo.GetByHopDongThangNamAsync(hopDongId, thang, nam);
        bool canSinhHd = soNgayO < soNgayTrongThang && hdThangNay == null;

        decimal tienPhongProRata = 0;
        decimal tongDichVuThangCuoi = 0;

        if (canSinhHd)
        {
            decimal giaPhong = await LayGiaPhongAsync(hd, thang, nam);
            tienPhongProRata = BillingPeriodCalculator.CalculateRoomCharge(giaPhong, soNgayO, soNgayTrongThang);

            await using var conn = new MySqlConnection(config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();
            var chiTietDv = await TinhChiTietDichVuAsync(conn, null, hd.PhongId, dvPhong, thang, nam);
            tongDichVuThangCuoi = chiTietDv.Sum(d => d.ThanhTien);
        }

        decimal tongNo = await hoaDonRepo.GetTongNoConLaiAsync(hopDongId);
        if (canSinhHd) tongNo += tienPhongProRata + tongDichVuThangCuoi;
        decimal soDuCoc = await giaoDichCocService.GetSoDuHienTaiAsync(hopDongId);
        decimal tienTruNoTuCoc = Math.Min(soDuCoc, Math.Max(0, tongNo));

        return new TraPhongViewModel
        {
            HopDongId = hopDongId,
            TenPhong = phong?.TenPhong ?? $"Phong #{hd.PhongId}",
            TenKhachChinh = khach?.HoTen ?? "(chua co khach)",
            TienCoc = soDuCoc,
            NgayTraPhong = ngayTraPhong,
            CanSinhHoaDonMoi = canSinhHd,
            SoNgayO = soNgayO,
            SoNgayTrongThang = soNgayTrongThang,
            TienPhongProRata = tienPhongProRata,
            TongTienDichVuThangCuoi = tongDichVuThangCuoi,
            TongNoConLai = tongNo,
            TienTruNoTuCoc = tienTruNoTuCoc,
            TienHoanCoc = soDuCoc - tongNo,
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
        var dvPhong = (await phongDvRepo.GetByPhongAsync(hd.PhongId)).ToList();

        int thang = ngayTraPhong.Month;
        int nam = ngayTraPhong.Year;
        int soNgayTrongThang = BillingPeriodCalculator.GetDaysInMonth(thang, nam);
        int soNgayO = BillingPeriodCalculator.CountOccupiedDays(thang, nam, hd.NgayBatDau, ngayTraPhong);

        if (soNgayO <= 0)
            throw new InvalidOperationException("Hop dong khong co ngay o trong ky tra phong.");

        var hdThangNay = await hoaDonRepo.GetByHopDongThangNamAsync(hopDongId, thang, nam);
        bool canSinhHd = soNgayO < soNgayTrongThang && hdThangNay == null;

        await using var conn = new MySqlConnection(config.GetConnectionString("DefaultConnection"));
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();

        try
        {
            int? hoaDonCuoiId = null;

            if (canSinhHd)
            {
                decimal giaPhong = await LayGiaPhongAsync(hd, thang, nam);
                decimal tienPhong = BillingPeriodCalculator.CalculateRoomCharge(giaPhong, soNgayO, soNgayTrongThang);

                decimal noKyTruoc = await TinhNoKyTruocAsync(conn, tx, hopDongId, thang, nam);

                var chiTietDv = await TinhChiTietDichVuAsync(conn, tx, hd.PhongId, dvPhong, thang, nam);
                decimal tongDv = chiTietDv.Sum(d => d.ThanhTien);
                decimal tongCong = tienPhong + tongDv + noKyTruoc;

                hoaDonCuoiId = await conn.ExecuteScalarAsync<int>(
                    """
                    INSERT INTO HoaDon
                        (HopDongId, Thang, Nam, NgayLap, TienPhong, TongTienDichVu,
                         TongCong, SoTienDaThu, TrangThaiThanhToan,
                         SoNgayO, SoNgayTrongThang, TienNoKyTruoc, GhiChu)
                    VALUES
                        (@HopDongId, @Thang, @Nam, NOW(), @TienPhong, @TongDV,
                         @TongCong, 0, 'ChuaThu',
                         @SoNgayO, @SoNgayTrongThang, @TienNoKyTruoc, @GhiChu);
                    SELECT LAST_INSERT_ID();
                    """,
                    new
                    {
                        HopDongId = hopDongId,
                        Thang = thang,
                        Nam = nam,
                        TienPhong = tienPhong,
                        TongDV = tongDv,
                        TongCong = tongCong,
                        SoNgayO = soNgayO,
                        SoNgayTrongThang = soNgayTrongThang,
                        TienNoKyTruoc = noKyTruoc,
                        GhiChu = $"Tra phong ngay {ngayTraPhong:dd/MM/yyyy}. {ghiChu}".Trim()
                    },
                    tx);

                await InsertChiTietAsync(conn, tx, hoaDonCuoiId.Value, chiTietDv);

                if (noKyTruoc > 0)
                {
                    var daKetChuyen = await congNoSettlementService.ThanhToanNoAsync(
                        conn,
                        tx,
                        hopDongId,
                        noKyTruoc,
                        ngayTraPhong,
                        "KetChuyenNo",
                        $"Ket chuyen no sang hoa don tra phong #{hoaDonCuoiId.Value}",
                        [hoaDonCuoiId.Value]);

                    if (daKetChuyen != noKyTruoc)
                        throw new InvalidOperationException("So tien no ky truoc khong khop voi cong no can ket chuyen.");
                }
            }

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

            await conn.ExecuteAsync(
                "UPDATE Phong SET TrangThai = 'Trong' WHERE Id = @Id",
                new { Id = hd.PhongId },
                tx);

            decimal tongNoTruocXuLyCoc = await TinhTongNoConLaiAsync(conn, tx, hopDongId);
            var ketQuaCoc = await giaoDichCocService.TatToanCocKhiTraPhongAsync(
                conn,
                tx,
                hd,
                hoaDonCuoiId,
                tongNoTruocXuLyCoc,
                ngayTraPhong,
                ghiChu);

            if (ketQuaCoc.SoTienTruNo > 0)
            {
                await congNoSettlementService.ThanhToanNoAsync(
                    conn,
                    tx,
                    hopDongId,
                    ketQuaCoc.SoTienTruNo,
                    ngayTraPhong,
                    "TruCoc",
                    $"Tru no vao coc khi tra phong hop dong #{hopDongId}");
            }

            decimal tongNoConLai = await TinhTongNoConLaiAsync(conn, tx, hopDongId);

            await conn.ExecuteAsync(
                "UPDATE HopDong SET TienCocHoanLai = @TienCocHoanLai WHERE Id = @Id",
                new { Id = hopDongId, TienCocHoanLai = ketQuaCoc.SoTienHoanCoc },
                tx);

            await tx.CommitAsync();

            return new KetQuaTraPhongViewModel
            {
                TenPhong = phong?.TenPhong ?? $"Phong #{hd.PhongId}",
                TenKhachChinh = khach?.HoTen ?? "",
                NgayTraPhong = ngayTraPhong,
                HoaDonCuoiId = hoaDonCuoiId,
                TienCoc = ketQuaCoc.SoDuCocTruocXuLy,
                TongNoConLai = tongNoConLai,
                TienTruNoTuCoc = ketQuaCoc.SoTienTruNo,
                TienHoanCoc = ketQuaCoc.SoTienHoanCoc - ketQuaCoc.KhachConNoThem,
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
                var chiSo = await LayChiSoAsync(conn, tx, phongId, dv.DichVuId, thang, nam);
                if (chiSo == null) continue;

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
        MySqlTransaction? tx,
        int phongId,
        int dichVuId,
        int thang,
        int nam)
    {
        const string sql = """
            SELECT *
            FROM ChiSoDienNuoc
            WHERE PhongId = @PhongId
              AND DichVuId = @DichVuId
              AND Thang = @Thang
              AND Nam = @Nam
            LIMIT 1
            """;

        return await conn.QueryFirstOrDefaultAsync<ChiSoDienNuoc>(
            sql,
            new { PhongId = phongId, DichVuId = dichVuId, Thang = thang, Nam = nam },
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
}
