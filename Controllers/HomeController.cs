using Dapper;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using QuanLyNhaTro.Models;
using QuanLyNhaTro.Repositories;
using QuanLyNhaTro.Services;

namespace QuanLyNhaTro.Controllers;

public class HomeController(
    IDbConnection db,
    PhongRepository phongRepo,
    HopDongService hopDongService) : Controller
{
    public async Task<IActionResult> Index()
    {
        await hopDongService.KichHoatHopDongDenHanAsync(DateTime.Today);
        ViewData["ActiveMenu"] = "dashboard";

        var ky = DefaultBillingPeriodResolver.Resolve();
        var thang = ky.Thang;
        var nam = ky.Nam;

        // ── Thống kê phòng ───────────────────────────────────────────────────
        var tatCaPhong = (await phongRepo.GetAllTheoTrangThaiHieuLucAsync(DateTime.Today)).ToList();

        // ── Hóa đơn kỳ này ──────────────────────────────────────────────────
        // Query thẳng DB cho gọn, tránh N+1
        const string sqlHoaDon = """
            SELECT hd.*, hdd.Id, hdd.PhongId, hdd.TrangThai AS TrangThaiHopDong,
                   p.Id, p.TenPhong
            FROM HoaDon hd
            INNER JOIN HopDong hdd ON hdd.Id = hd.HopDongId
            INNER JOIN Phong p     ON p.Id   = hdd.PhongId
            WHERE hd.Thang = @Thang AND hd.Nam = @Nam
            ORDER BY hd.TrangThaiThanhToan, p.TenPhong
            """;

        var hoaDonKy = (await db.QueryAsync<HoaDon, HopDong, Phong, HoaDon>(
            sqlHoaDon,
            (hd, hopDong, phong) => { hopDong.Phong = phong; hd.HopDong = hopDong; return hd; },
            new { Thang = thang, Nam = nam },
            splitOn: "Id,Id")).ToList();

        // ── Hợp đồng sắp hết hạn (≤ 30 ngày) ───────────────────────────────
        var ngayGioiHan = DateTime.Today.AddDays(30);
        const string sqlSapHet = """
            SELECT hd.*, p.Id, p.TenPhong
            FROM HopDong hd
            INNER JOIN Phong p ON p.Id = hd.PhongId
            WHERE hd.TrangThai = 'DangHieuLuc'
              AND hd.NgayKetThuc IS NOT NULL
              AND hd.NgayKetThuc <= @NgayGioiHan
            ORDER BY hd.NgayKetThuc
            """;
        var sapHetHan = (await db.QueryAsync<HopDong, Phong, HopDong>(
            sqlSapHet,
            (hd, p) => { hd.Phong = p; return hd; },
            new { NgayGioiHan = ngayGioiHan },
            splitOn: "Id")).ToList();

        var vm = new DashboardViewModel
        {
            Thang              = thang,
            Nam                = nam,
            TongSoPhong        = tatCaPhong.Count,
            SoPhongDangThue    = tatCaPhong.Count(p => p.TrangThai == "DangThue"),
            SoPhongTrong       = tatCaPhong.Count(p => p.TrangThai == "Trong"),
            SoPhongDangSuaChua = tatCaPhong.Count(p => p.TrangThai == "DangSuaChua"),
            TongPhaiThu        = hoaDonKy.Sum(h => h.TongCong),
            TongDaThu          = hoaDonKy.Sum(h => h.SoTienDaThu),
            SoHoaDonChuaThu    = hoaDonKy.Count(h => h.TrangThaiThanhToan != "DaThu"),
            SoHoaDonDaThu      = hoaDonKy.Count(h => h.TrangThaiThanhToan == "DaThu"),
            HoaDonChuaThu      = hoaDonKy.Where(h => h.TrangThaiThanhToan != "DaThu").ToList(),
            PhongTrong         = tatCaPhong.Where(p => p.TrangThai == "Trong").ToList(),
            HopDongSapHetHan   = sapHetHan,
        };

        return View(vm);
    }

    public IActionResult Error() => View();
}
