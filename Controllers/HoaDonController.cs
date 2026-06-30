using Microsoft.AspNetCore.Mvc;
using QuanLyNhaTro.Models;
using QuanLyNhaTro.Repositories;
using QuanLyNhaTro.Services;

namespace QuanLyNhaTro.Controllers;

public class HoaDonController(
    HoaDonRepository hoaDonRepo,
    ChiTietHoaDonRepository chiTietRepo,
    ThanhToanRepository thanhToanRepo,
    HopDongRepository hopDongRepo,
    KhachThueRepository khachRepo,
    PhongRepository phongRepo,
    HoaDonService hoaDonService,
    ExcelService excelService) : Controller
{
    // GET /HoaDon?thang=6&nam=2026
    public async Task<IActionResult> Index(int? thang, int? nam)
    {
        ViewData["ActiveMenu"] = "hoadon";
        thang ??= DateTime.Today.Month;
        nam   ??= DateTime.Today.Year;

        // Hóa đơn của kỳ đang xem
        var hopDongs = (await hopDongRepo.GetAllAsync()).ToList();
        var danhSach = new List<HoaDon>();
        foreach (var hd in hopDongs)
        {
            var hdKy = await hoaDonRepo.GetByHopDongKyAsync(hd.Id, thang.Value, nam.Value);
            if (hdKy != null)
            {
                hdKy.HopDong = hd;
                hdKy.DanhSachThanhToan = (await thanhToanRepo.GetByHoaDonAsync(hdKy.Id)).ToList();
                danhSach.Add(hdKy);
            }
        }

        ViewBag.Thang = thang;
        ViewBag.Nam   = nam;
        return View(danhSach);
    }

    // GET /HoaDon/ChotHangLoat?thang=6&nam=2026
    public async Task<IActionResult> ChotHangLoat(int? thang, int? nam)
    {
        ViewData["ActiveMenu"] = "hoadon";
        thang ??= DateTime.Today.Month;
        nam ??= DateTime.Today.Year;

        var model = await BuildChotHangLoatPreviewAsync(thang.Value, nam.Value);
        return View(model);
    }

    // POST /HoaDon/ChotHangLoat
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChotHangLoat(int thang, int nam, int[] hopDongIds)
    {
        if (hopDongIds.Length == 0)
        {
            TempData["Error"] = "Chưa chọn hợp đồng nào để chốt.";
            return RedirectToAction(nameof(ChotHangLoat), new { thang, nam });
        }

        var daTao = 0;
        var boQua = 0;
        var loi = new List<string>();

        foreach (var hopDongId in hopDongIds.Distinct())
        {
            var duKien = await hoaDonService.TinhHoaDonDuKienAsync(hopDongId, thang, nam);
            if (!duKien.SanSangChot)
            {
                boQua++;
                continue;
            }

            try
            {
                await hoaDonService.LapHoaDonAsync(hopDongId, thang, nam);
                daTao++;
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                boQua++;
                var tenPhong = duKien.HopDong?.Phong?.TenPhong ?? $"HĐ #{hopDongId}";
                loi.Add($"{tenPhong}: {ex.Message}");
            }
        }

        if (daTao > 0)
            TempData["Success"] = $"Đã chốt {daTao} hóa đơn kỳ {thang}/{nam}.";

        if (boQua > 0)
        {
            var thongBao = $"Bỏ qua {boQua} dòng chưa sẵn sàng hoặc đã có hóa đơn.";
            if (loi.Count > 0)
                thongBao += " " + string.Join(" ", loi.Take(3));
            TempData["Error"] = thongBao;
        }

        return RedirectToAction(nameof(ChotHangLoat), new { thang, nam });
    }

    // GET /HoaDon/Details/5
    public async Task<IActionResult> Details(int id)
    {
        ViewData["ActiveMenu"] = "hoadon";
        var hd = await hoaDonRepo.GetByIdAsync(id);
        if (hd == null) return NotFound();

        hd.HopDong       = await hopDongRepo.GetByIdAsync(hd.HopDongId);
        hd.ChiTiet       = (await chiTietRepo.GetByHoaDonAsync(id)).ToList();
        hd.DanhSachThanhToan = (await thanhToanRepo.GetByHoaDonAsync(id)).ToList();

        return View(hd);
    }

    // GET /HoaDon/Create?hopDongId=1
    public async Task<IActionResult> Create(int hopDongId)
    {
        ViewData["ActiveMenu"] = "hoadon";
        var hopDong = await hopDongRepo.GetByIdAsync(hopDongId);
        if (hopDong == null) return NotFound();

        ViewBag.HopDong = hopDong;
        ViewBag.Thang   = DateTime.Today.Month;
        ViewBag.Nam     = DateTime.Today.Year;
        return View();
    }

    // POST /HoaDon/Create
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int hopDongId, int thang, int nam)
    {
        try
        {
            var hoaDonId = await hoaDonService.LapHoaDonAsync(hopDongId, thang, nam);
            TempData["Success"] = $"Đã lập hóa đơn kỳ {thang}/{nam}.";
            return RedirectToAction(nameof(Details), new { id = hoaDonId });
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Create), new { hopDongId });
        }
    }

    // GET /HoaDon/ThuTien/5
    public async Task<IActionResult> ThuTien(int id)
    {
        ViewData["ActiveMenu"] = "hoadon";
        var hd = await hoaDonRepo.GetByIdAsync(id);
        if (hd == null) return NotFound();

        hd.HopDong           = await hopDongRepo.GetByIdAsync(hd.HopDongId);
        hd.DanhSachThanhToan = (await thanhToanRepo.GetByHoaDonAsync(id)).ToList();
        return View(hd);
    }

    // POST /HoaDon/ThuTien
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ThuTien(
        int hoaDonId,
        decimal soTien,
        string hinhThuc,
        string? ghiChu,
        string? returnTo,
        int? thang,
        int? nam)
    {
        try
        {
            await hoaDonService.ThuTienAsync(hoaDonId, soTien, hinhThuc, ghiChu);
            TempData["Success"] = $"Đã ghi nhận thu {soTien:N0} đ.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        if (returnTo == "Index")
        {
            return RedirectToAction(nameof(Index), new { thang, nam });
        }

        return RedirectToAction(nameof(Details), new { id = hoaDonId });
    }

    // POST /HoaDon/Delete/5 — chỉ cho phép xoá hóa đơn chưa thu
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await hoaDonService.XoaHoaDonAsync(id);
            TempData["Success"] = "Đã xoá hóa đơn.";
            return RedirectToAction(nameof(Index));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }

        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }

    }

    // GET /HoaDon/XuatPhieuThu/5
    public async Task<IActionResult> XuatPhieuThu(int id)
    {
        var hd = await hoaDonRepo.GetByIdAsync(id);
        if (hd == null) return NotFound();

        var hopDong   = await hopDongRepo.GetByIdAsync(hd.HopDongId);
        if (hopDong == null) return NotFound();

        var phong     = await phongRepo.GetByIdAsync(hopDong.PhongId);
        var khach     = await khachRepo.GetByHopDongAsync(hd.HopDongId);
        var chiTiet   = await chiTietRepo.GetByHoaDonAsync(id);
        var thanhToan = await thanhToanRepo.GetByHoaDonAsync(id);

        var bytes = excelService.XuatPhieuThu(
            hd, hopDong, phong!, khach, chiTiet, thanhToan);

        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"PhieuThu_T{hd.Thang}_{hd.Nam}_{phong?.TenPhong}.xlsx");
    }

    private async Task<HoaDonHangLoatPreviewViewModel> BuildChotHangLoatPreviewAsync(int thang, int nam)
    {
        var hopDongs = (await hopDongRepo.GetAllAsync())
            .Where(hd => hd.TrangThai == "DangHieuLuc")
            .OrderBy(hd => hd.Phong?.TenPhong)
            .ThenBy(hd => hd.Id)
            .ToList();

        var rows = new List<HoaDonDuKien>();
        foreach (var hopDong in hopDongs)
        {
            rows.Add(await hoaDonService.TinhHoaDonDuKienAsync(hopDong.Id, thang, nam));
        }

        return new HoaDonHangLoatPreviewViewModel
        {
            Thang = thang,
            Nam = nam,
            Rows = rows
        };
    }
}
