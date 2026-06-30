using System.Data;
using Dapper;
using MySqlConnector;
using QuanLyNhaTro.Models;
using QuanLyNhaTro.Repositories;

namespace QuanLyNhaTro.Services;

public class HoaDonService(
    IDbConnection db,
    HoaDonRepository hoaDonRepo,
    ChiTietHoaDonRepository chiTietRepo,
    ThanhToanRepository thanhToanRepo,
    HopDongRepository hopDongRepo,
    PhongDichVuRepository phongDichVuRepo,
    ChiSoDienNuocRepository chiSoRepo,
    LichSuThayDoiGiaRepository lichSuGiaRepo,
    CongNoSettlementService congNoSettlementService)
{
    public async Task<decimal> LayGiaApDungAsync(
        string loaiDoiTuong,
        int doiTuongId,
        int thang,
        int nam,
        decimal giaHienTai)
    {
        var lichSu = await lichSuGiaRepo.GetGiaApDungAsync(loaiDoiTuong, doiTuongId, thang, nam);
        return lichSu?.GiaMoi ?? giaHienTai;
    }

    public async Task<int> LapHoaDonAsync(
        int hopDongId,
        int thang,
        int nam,
        int? soNgayO = null,
        int? soNgayTrongThang = null,
        int? hoaDonGhepId = null,
        string? ghiChu = null)
    {
        var existing = await hoaDonRepo.GetByHopDongKyAsync(hopDongId, thang, nam);
        if (existing != null)
            throw new InvalidOperationException($"Hoa don ky {thang}/{nam} cua hop dong #{hopDongId} da ton tai.");

        var hopDong = await hopDongRepo.GetByIdAsync(hopDongId)
            ?? throw new InvalidOperationException($"Khong tim thay hop dong #{hopDongId}.");

        var (soNgayTinhTien, soNgayTrongThangTinhTien) = ResolveSoNgayTinhTien(
            hopDong,
            thang,
            nam,
            soNgayO,
            soNgayTrongThang);

        var giaPhong = await LayGiaApDungAsync("Phong", hopDong.PhongId, thang, nam, hopDong.TienThueThoaThuan);
        var tienPhong = soNgayTinhTien.HasValue && soNgayTrongThangTinhTien.HasValue
            ? BillingPeriodCalculator.CalculateRoomCharge(giaPhong, soNgayTinhTien.Value, soNgayTrongThangTinhTien.Value)
            : giaPhong;

        var chiTietList = new List<ChiTietHoaDon>();
        decimal tongTienDV = 0;
        var danhSachDV = await phongDichVuRepo.GetByPhongAsync(hopDong.PhongId);

        foreach (var pdv in danhSachDV)
        {
            if (pdv.DichVu == null) continue;

            if (pdv.DichVu.LoaiTinhPhi == "TheoChiSo")
            {
                var chiSo = (await chiSoRepo.GetByHopDongKyAsync(hopDongId, thang, nam))
                    .FirstOrDefault(cs => cs.DichVuId == pdv.DichVuId);

                if (chiSo == null) continue;

                var donGia = await LayGiaApDungAsync("DichVu", pdv.Id, thang, nam, pdv.DonGia);
                var soLuong = ChiSoConsumptionCalculator.Calculate(chiSo);
                var thanhTien = Math.Round(soLuong * donGia, 0);

                chiTietList.Add(new ChiTietHoaDon
                {
                    DichVuId = pdv.DichVuId,
                    SoLuong = soLuong,
                    DonGia = donGia,
                    ThanhTien = thanhTien,
                    ChiSoDienNuocId = chiSo.Id
                });
                tongTienDV += thanhTien;
            }
            else
            {
                if (hoaDonGhepId.HasValue && ghiChu == "PHONG_CU") continue;

                var donGia = await LayGiaApDungAsync("DichVu", pdv.Id, thang, nam, pdv.DonGia);
                var thanhTien = Math.Round(donGia, 0);

                chiTietList.Add(new ChiTietHoaDon
                {
                    DichVuId = pdv.DichVuId,
                    SoLuong = 1,
                    DonGia = donGia,
                    ThanhTien = thanhTien
                });
                tongTienDV += thanhTien;
            }
        }

        var tienNoKyTruoc = await TinhNoKyTruocAsync(hopDong, thang, nam);
        var tongCong = tienPhong + tongTienDV + tienNoKyTruoc;

        var hoaDon = new HoaDon
        {
            HopDongId = hopDongId,
            Thang = thang,
            Nam = nam,
            NgayLap = DateTime.Now,
            TienPhong = tienPhong,
            TongTienDichVu = tongTienDV,
            TienNoKyTruoc = tienNoKyTruoc,
            TongCong = tongCong,
            SoTienDaThu = 0,
            TrangThaiThanhToan = "ChuaThu",
            SoNgayO = soNgayTinhTien,
            SoNgayTrongThang = soNgayTrongThangTinhTien,
            HoaDonGhepId = hoaDonGhepId,
            GhiChu = ghiChu == "PHONG_CU" ? null : ghiChu
        };

        var conn = (MySqlConnection)db;
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync();

        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            var existingInTransaction = await hoaDonRepo.GetByHopDongKyAsync(conn, tx, hopDongId, thang, nam);
            if (existingInTransaction != null)
                throw new InvalidOperationException($"Hoa don ky {thang}/{nam} cua hop dong #{hopDongId} da ton tai.");

            var hoaDonId = await hoaDonRepo.InsertAsync(conn, tx, hoaDon);

            foreach (var ct in chiTietList)
            {
                ct.HoaDonId = hoaDonId;
                await chiTietRepo.InsertAsync(conn, tx, ct);
            }

            if (tienNoKyTruoc > 0)
            {
                var daKetChuyen = await congNoSettlementService.ThanhToanNoAsync(
                    conn,
                    tx,
                    hopDongId,
                    tienNoKyTruoc,
                    hoaDon.NgayLap,
                    "KetChuyenNo",
                    $"Ket chuyen no sang hoa don #{hoaDonId}",
                    [hoaDonId]);

                if (daKetChuyen != tienNoKyTruoc)
                    throw new InvalidOperationException("So tien no ky truoc khong khop voi cong no can ket chuyen.");
            }

            await tx.CommitAsync();
            return hoaDonId;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task ThuTienAsync(int hoaDonId, decimal soTien, string hinhThuc, string? ghiChu = null)
    {
        if (soTien <= 0)
            throw new ArgumentException("So tien thu phai lon hon 0.");

        if (hinhThuc is not ("TienMat" or "ChuyenKhoan"))
            throw new ArgumentException("Hinh thuc thu tien khong hop le.");

        var hoaDon = await hoaDonRepo.GetByIdAsync(hoaDonId)
            ?? throw new InvalidOperationException($"Khong tim thay hoa don #{hoaDonId}.");

        var conLai = hoaDon.TongCong - hoaDon.SoTienDaThu;
        if (conLai <= 0)
            throw new InvalidOperationException("Hoa don da thu du.");

        if (soTien > conLai)
            throw new InvalidOperationException($"So tien thu khong duoc vuot qua so con lai {conLai:N0} d.");

        var conn = (MySqlConnection)db;
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync();

        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            var thanhToan = new ThanhToan
            {
                HoaDonId = hoaDonId,
                SoTien = soTien,
                NgayThu = DateTime.Now,
                HinhThuc = hinhThuc,
                GhiChu = ghiChu
            };
            await thanhToanRepo.InsertAsync(conn, tx, thanhToan);

            var soTienDaThuMoi = hoaDon.SoTienDaThu + soTien;
            var trangThai = soTienDaThuMoi <= 0 ? "ChuaThu"
                : soTienDaThuMoi < hoaDon.TongCong ? "ThuMotPhan"
                : "DaThu";

            await hoaDonRepo.UpdateSoTienDaThuAsync(conn, tx, hoaDonId, soTienDaThuMoi, trangThai);

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task XoaHoaDonAsync(int hoaDonId)
    {
        var conn = (MySqlConnection)db;
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync();

        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            var hoaDon = await hoaDonRepo.GetByIdAsync(conn, tx, hoaDonId)
                ?? throw new KeyNotFoundException($"Khong tim thay hoa don #{hoaDonId}.");

            if (hoaDon.SoTienDaThu > 0)
                throw new InvalidOperationException("Khong the xoa hoa don da co giao dich thu tien.");

            if (hoaDon.TienNoKyTruoc > 0)
                throw new InvalidOperationException("Khong the xoa hoa don dang mang no ky truoc da ket chuyen.");

            await chiTietRepo.DeleteByHoaDonAsync(conn, tx, hoaDonId);
            await hoaDonRepo.DeleteAsync(conn, tx, hoaDonId);

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private static (int? SoNgayO, int? SoNgayTrongThang) ResolveSoNgayTinhTien(
        HopDong hopDong,
        int thang,
        int nam,
        int? soNgayO,
        int? soNgayTrongThang)
    {
        if (soNgayO.HasValue != soNgayTrongThang.HasValue)
            throw new InvalidOperationException("Phai truyen dong thoi SoNgayO va SoNgayTrongThang khi lap hoa don khong tron thang.");

        var soNgayTrongThangThucTe = BillingPeriodCalculator.GetDaysInMonth(thang, nam);

        if (soNgayO.HasValue)
        {
            if (soNgayTrongThang != soNgayTrongThangThucTe)
                throw new InvalidOperationException($"So ngay trong thang {thang}/{nam} phai la {soNgayTrongThangThucTe}.");

            if (soNgayO.Value <= 0)
                throw new InvalidOperationException("So ngay o phai lon hon 0.");

            return (soNgayO, soNgayTrongThang);
        }

        var soNgayTheoHopDong = BillingPeriodCalculator.CountOccupiedDays(
            thang,
            nam,
            hopDong.NgayBatDau,
            hopDong.NgayKetThuc);

        if (soNgayTheoHopDong <= 0)
            throw new InvalidOperationException($"Hop dong #{hopDong.Id} khong co ngay o trong ky {thang}/{nam}.");

        return soNgayTheoHopDong == soNgayTrongThangThucTe
            ? (null, null)
            : (soNgayTheoHopDong, soNgayTrongThangThucTe);
    }

    private async Task<decimal> TinhNoKyTruocAsync(HopDong hopDong, int thang, int nam)
    {
        var noTruocKy = await TinhTongNoTruocKyAsync(hopDong.Id, thang, nam);
        if (noTruocKy > 0)
            return noTruocKy;

        var kyTruoc = await hoaDonRepo.GetKyTruocAsync(hopDong.Id, thang, nam);
        var duKyTruoc = kyTruoc?.TongCong - kyTruoc?.SoTienDaThu;
        if (duKyTruoc < 0)
            return duKyTruoc.Value;

        if (hopDong.HopDongTruocId.HasValue)
        {
            var hoaDonCuoi = (await hoaDonRepo.GetByHopDongAsync(hopDong.HopDongTruocId.Value)).FirstOrDefault();
            if (hoaDonCuoi != null)
                return hoaDonCuoi.TongCong - hoaDonCuoi.SoTienDaThu;
        }

        return 0;
    }

    private async Task<decimal> TinhTongNoTruocKyAsync(int hopDongId, int thang, int nam)
        => await db.ExecuteScalarAsync<decimal>(
            """
            SELECT COALESCE(SUM(TongCong - SoTienDaThu), 0)
            FROM HoaDon
            WHERE HopDongId = @HopDongId
              AND TongCong > SoTienDaThu
              AND (Nam < @Nam OR (Nam = @Nam AND Thang < @Thang))
            """,
            new { HopDongId = hopDongId, Thang = thang, Nam = nam });
}
