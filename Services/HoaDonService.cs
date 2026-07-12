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
    HopDongDichVuRepository hopDongDichVuRepo,
    ChiSoDienNuocRepository chiSoRepo,
    KhoanPhatSinhHopDongRepository khoanPhatSinhRepo,
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
        var giaTheoKy = await lichSuGiaRepo.GetGiaTriApDungAsync(loaiDoiTuong, doiTuongId, thang, nam);
        return giaTheoKy ?? giaHienTai;
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
        var duKien = await TinhHoaDonDuKienAsync(hopDongId, thang, nam, soNgayO, soNgayTrongThang, hoaDonGhepId, ghiChu);
        if (duKien.HoaDonDaCo != null)
            throw new InvalidOperationException($"Hoa don ky {thang}/{nam} cua hop dong #{hopDongId} da ton tai.");

        if (duKien.Loi.Count > 0)
            throw new InvalidOperationException(string.Join(" ", duKien.Loi));

        var hoaDon = new HoaDon
        {
            HopDongId = hopDongId,
            Thang = thang,
            Nam = nam,
            NgayLap = DateTime.Now,
            TienPhong = duKien.TienPhong,
            TongTienDichVu = duKien.TongTienDichVu,
            TongTienPhatSinh = duKien.TongTienPhatSinh,
            TienNoKyTruoc = duKien.TienNoKyTruoc,
            TongCong = duKien.TongCong,
            SoTienDaThu = 0,
            TrangThaiThanhToan = "ChuaThu",
            SoNgayO = duKien.SoNgayO,
            SoNgayTrongThang = duKien.SoNgayTrongThang,
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

            foreach (var ct in duKien.ChiTiet)
            {
                await chiTietRepo.InsertAsync(conn, tx, new ChiTietHoaDon
                {
                    HoaDonId = hoaDonId,
                    DichVuId = ct.DichVuId,
                    ChiSoDienNuocId = ct.ChiSoDienNuocId,
                    SoLuong = ct.SoLuong,
                    DonGia = ct.DonGia,
                    ThanhTien = ct.ThanhTien
                });
            }

            await khoanPhatSinhRepo.GanVaoHoaDonAsync(
                conn,
                tx,
                duKien.KhoanPhatSinh.Select(x => x.Id),
                hoaDonId);

            if (duKien.TienNoKyTruoc > 0)
            {
                var daKetChuyen = await congNoSettlementService.ThanhToanNoAsync(
                    conn,
                    tx,
                    hopDongId,
                    duKien.TienNoKyTruoc,
                    hoaDon.NgayLap,
                    "KetChuyenNo",
                    $"Ket chuyen no sang hoa don #{hoaDonId}",
                    [hoaDonId]);

                if (daKetChuyen != duKien.TienNoKyTruoc)
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

    public async Task<HoaDonDuKien> TinhHoaDonDuKienAsync(
        int hopDongId,
        int thang,
        int nam,
        int? soNgayO = null,
        int? soNgayTrongThang = null,
        int? hoaDonGhepId = null,
        string? ghiChu = null)
    {
        var result = new HoaDonDuKien
        {
            HopDongId = hopDongId,
            Thang = thang,
            Nam = nam,
            HoaDonDaCo = await hoaDonRepo.GetByHopDongKyAsync(hopDongId, thang, nam)
        };

        var hopDong = await hopDongRepo.GetByIdAsync(hopDongId);
        if (hopDong == null)
        {
            result.Loi.Add($"Khong tim thay hop dong #{hopDongId}.");
            return result;
        }

        result.HopDong = hopDong;

        try
        {
            var (soNgayTinhTien, soNgayTrongThangTinhTien) = ResolveSoNgayTinhTien(
                hopDong,
                thang,
                nam,
                soNgayO,
                soNgayTrongThang);

            result.SoNgayO = soNgayTinhTien;
            result.SoNgayTrongThang = soNgayTrongThangTinhTien;

            var giaPhong = await LayGiaApDungAsync("HopDong", hopDong.Id, thang, nam, hopDong.TienThueThoaThuan);
            result.TienPhong = soNgayTinhTien.HasValue && soNgayTrongThangTinhTien.HasValue
                ? BillingPeriodCalculator.CalculateRoomCharge(giaPhong, soNgayTinhTien.Value, soNgayTrongThangTinhTien.Value)
                : giaPhong;
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            result.Loi.Add(ex.Message);
            return result;
        }

        var danhSachDV = (await hopDongDichVuRepo.GetPhongDichVuByHopDongKyAsync(
            hopDongId, thang, nam)).ToList();
        if (danhSachDV.Count == 0)
            result.CanhBao.Add("Hop dong chua dang ky dich vu nao trong ky.");

        var chiSoTheoKy = (await chiSoRepo.GetByHopDongKyAsync(hopDongId, thang, nam)).ToList();

        foreach (var pdv in danhSachDV)
        {
            if (pdv.DichVu == null) continue;

            var donGia = await LayGiaApDungAsync("DichVu", pdv.Id, thang, nam, pdv.DonGia);

            if (pdv.DichVu.LoaiTinhPhi == "TheoChiSo")
            {
                var chiSo = chiSoTheoKy.FirstOrDefault(cs => cs.DichVuId == pdv.DichVuId);
                if (chiSo == null)
                {
                    result.Loi.Add($"Thieu chi so {pdv.DichVu.TenDichVu}.");
                    continue;
                }

                try
                {
                    var soLuong = ChiSoConsumptionCalculator.Calculate(chiSo);
                    var thanhTien = Math.Round(soLuong * donGia, 0);
                    result.ChiTiet.Add(new HoaDonDuKienChiTiet
                    {
                        DichVuId = pdv.DichVuId,
                        PhongDichVuId = pdv.Id,
                        ChiSoDienNuocId = chiSo.Id,
                        TenDichVu = pdv.DichVu.TenDichVu,
                        LoaiTinhPhi = pdv.DichVu.LoaiTinhPhi,
                        SoLuong = soLuong,
                        DonGia = donGia,
                        ThanhTien = thanhTien
                    });
                    result.TongTienDichVu += thanhTien;
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
                {
                    result.Loi.Add($"{pdv.DichVu.TenDichVu}: {ex.Message}");
                }
            }
            else
            {
                if (hoaDonGhepId.HasValue && ghiChu == "PHONG_CU") continue;

                decimal soLuong;
                try
                {
                    soLuong = await FixedServiceQuantityCalculator.ResolveQuantityAsync(db, null, hopDongId, pdv.DichVu);
                }
                catch (InvalidOperationException ex)
                {
                    result.Loi.Add(ex.Message);
                    continue;
                }

                var thanhTien = Math.Round(soLuong * donGia, 0);
                result.ChiTiet.Add(new HoaDonDuKienChiTiet
                {
                    DichVuId = pdv.DichVuId,
                    PhongDichVuId = pdv.Id,
                    TenDichVu = pdv.DichVu.TenDichVu,
                    LoaiTinhPhi = pdv.DichVu.LoaiTinhPhi,
                    CachTinhCoDinh = pdv.DichVu.CachTinhCoDinh,
                    SoLuong = soLuong,
                    DonGia = donGia,
                    ThanhTien = thanhTien
                });
                result.TongTienDichVu += thanhTien;
            }
        }

        var denNgay = new DateTime(nam, thang, BillingPeriodCalculator.GetDaysInMonth(thang, nam));
        var khoanPhatSinh = await khoanPhatSinhRepo.GetChuaXuLyDenNgayAsync(hopDongId, denNgay);
        foreach (var khoan in khoanPhatSinh)
        {
            result.KhoanPhatSinh.Add(new HoaDonDuKienKhoanPhatSinh
            {
                Id = khoan.Id,
                NgayPhatSinh = khoan.NgayPhatSinh,
                LoaiKhoan = khoan.LoaiKhoan,
                MoTa = khoan.MoTa,
                SoTien = khoan.SoTien,
                SoTienConLai = khoan.SoTienConLai
            });
            result.TongTienPhatSinh += khoan.SoTienConLai;
        }

        if (result.TongTienPhatSinh > 0)
            result.CanhBao.Add($"Co khoan phat sinh {result.TongTienPhatSinh:N0} d.");

        result.TienNoKyTruoc = await TinhNoKyTruocAsync(hopDong, thang, nam);
        if (result.TienNoKyTruoc > 0)
            result.CanhBao.Add($"Co no ky truoc {result.TienNoKyTruoc:N0} d.");

        result.TongCong = result.TienPhong + result.TongTienDichVu + result.TongTienPhatSinh + result.TienNoKyTruoc;
        return result;
    }

    public async Task ThuTienAsync(int hoaDonId, decimal soTien, string hinhThuc, string? ghiChu = null)
    {
        if (soTien <= 0)
            throw new ArgumentException("So tien thu phai lon hon 0.");

        if (hinhThuc is not ("TienMat" or "ChuyenKhoan"))
            throw new ArgumentException("Hinh thuc thu tien khong hop le.");

        var conn = (MySqlConnection)db;
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync();

        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            var hoaDon = await hoaDonRepo.GetByIdForUpdateAsync(conn, tx, hoaDonId)
                ?? throw new InvalidOperationException($"Khong tim thay hoa don #{hoaDonId}.");

            var conLai = hoaDon.TongCong - hoaDon.SoTienDaThu;
            if (conLai <= 0)
                throw new InvalidOperationException("Hoa don da thu du.");

            if (soTien > conLai)
                throw new InvalidOperationException($"So tien thu khong duoc vuot qua so con lai {conLai:N0} d.");

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
