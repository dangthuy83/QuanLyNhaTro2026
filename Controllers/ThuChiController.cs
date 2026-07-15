using Microsoft.AspNetCore.Mvc;
using QuanLyNhaTro.Models;
using QuanLyNhaTro.Repositories;
using QuanLyNhaTro.Services;

namespace QuanLyNhaTro.Controllers;

public class ThuChiController(ThuChiRepository repo, ThuChiService service, ExcelService excel) : Controller
{
    public async Task<IActionResult> Index(int? nam, int? thang, string? loai)
    {
        nam ??= DateTime.Today.Year;
        thang ??= DateTime.Today.Month;
        var ds = await repo.GetAllAsync(nam, thang, loai);
        var (tongThu, tongChi) = await repo.GetTongTheoThangAsync(thang.Value, nam.Value);
        ViewBag.Nam = nam;
        ViewBag.Thang = thang;
        ViewBag.Loai = loai ?? "";
        ViewBag.TongThu = tongThu;
        ViewBag.TongChi = tongChi;
        ViewBag.CanDoiThu = tongThu - tongChi;
        ViewBag.KySo = await repo.GetKySoAsync(thang.Value, nam.Value);
        return View(ds);
    }

    public async Task<IActionResult> Create(int? dieuChinhChoId)
    {
        if (!dieuChinhChoId.HasValue)
            return View(new ThuChi { NgayPhatSinh = DateTime.Today, LoaiGiaoDich = "Chi" });
        var original = await repo.GetByIdAsync(dieuChinhChoId.Value);
        if (original == null) return NotFound();
        var period = await repo.GetKySoAsync(original.NgayPhatSinh.Month, original.NgayPhatSinh.Year);
        if (period?.DaKhoa != true)
        {
            TempData["Error"] = "Tháng gốc chưa khóa; hãy sửa giao dịch trực tiếp.";
            return RedirectToAction(nameof(Edit), new { id = original.Id });
        }
        return View(new ThuChi
        {
            ThuChiGocId = original.Id,
            NgayPhatSinh = DateTime.Today,
            LoaiGiaoDich = original.LoaiGiaoDich == "Thu" ? "Chi" : "Thu",
            DanhMuc = original.DanhMuc,
            SoTien = original.SoTien,
            NoiDung = $"Điều chỉnh giao dịch #{original.Id} kỳ {original.NgayPhatSinh:MM/yyyy}",
            GhiChu = $"Đảo giao dịch gốc #{original.Id}; kỳ gốc {original.NgayPhatSinh:MM/yyyy}."
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ThuChi tc)
    {
        if (!ModelState.IsValid) return View(tc);
        try
        {
            await service.CreateAsync(tc);
            TempData["Success"] = tc.LaDieuChinh ? "Đã ghi bút toán điều chỉnh." : "Đã thêm giao dịch.";
            return RedirectToAction(nameof(Index), new { thang = tc.NgayPhatSinh.Month, nam = tc.NgayPhatSinh.Year });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(tc);
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        var tc = await repo.GetByIdAsync(id);
        if (tc == null) return NotFound();
        var period = await repo.GetKySoAsync(tc.NgayPhatSinh.Month, tc.NgayPhatSinh.Year);
        if (period?.DaKhoa == true || tc.LaDieuChinh)
        {
            TempData["Error"] = period?.DaKhoa == true
                ? "Tháng đã khóa sổ; hãy tạo bút toán điều chỉnh trong tháng đang mở."
                : "Bút toán điều chỉnh không được sửa.";
            return RedirectToAction(nameof(Index), new { thang = tc.NgayPhatSinh.Month, nam = tc.NgayPhatSinh.Year });
        }
        return View(tc);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ThuChi tc)
    {
        if (!ModelState.IsValid) return View(tc);
        try
        {
            await service.UpdateAsync(tc);
            TempData["Success"] = "Đã cập nhật giao dịch.";
            return RedirectToAction(nameof(Index), new { thang = tc.NgayPhatSinh.Month, nam = tc.NgayPhatSinh.Year });
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(tc);
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int? thang, int? nam)
    {
        try { await service.DeleteAsync(id); TempData["Success"] = "Đã xóa giao dịch."; }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index), new { thang, nam });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> KhoaSo(int thang, int nam, string? ghiChu)
    {
        try { await service.LockPeriodAsync(thang, nam, ghiChu); TempData["Success"] = $"Đã khóa sổ tháng {thang}/{nam}."; }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index), new { thang, nam });
    }

    public async Task<IActionResult> XuatExcel(int nam, int thang)
    {
        var ds = await repo.GetAllAsync(nam, thang, null);
        var (tongThu, tongChi) = await repo.GetTongTheoThangAsync(thang, nam);
        var bytes = excel.XuatThuChi(ds, thang, nam, tongThu, tongChi);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"ThuChi_T{thang}_{nam}.xlsx");
    }
}
