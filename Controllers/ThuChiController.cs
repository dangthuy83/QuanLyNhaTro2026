using Microsoft.AspNetCore.Mvc;
using QuanLyNhaTro.Models;
using QuanLyNhaTro.Repositories;
using QuanLyNhaTro.Services;

namespace QuanLyNhaTro.Controllers;

public class ThuChiController(ThuChiRepository repo, ExcelService excel) : Controller
{
    public async Task<IActionResult> Index(int? nam, int? thang, string? loai)
    {
        nam   ??= DateTime.Today.Year;
        thang ??= DateTime.Today.Month;

        var ds = await repo.GetAllAsync(nam, thang, loai);
        var (tongThu, tongChi) = await repo.GetTongTheoThangAsync(thang.Value, nam.Value);

        ViewBag.Nam       = nam;
        ViewBag.Thang     = thang;
        ViewBag.Loai      = loai ?? "";
        ViewBag.TongThu   = tongThu;
        ViewBag.TongChi   = tongChi;
        ViewBag.CanDoiThu = tongThu - tongChi;

        return View(ds);
    }

    public IActionResult Create() => View(new ThuChi { NgayPhatSinh = DateTime.Today, LoaiGiaoDich = "Chi" });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ThuChi tc)
    {
        if (!ModelState.IsValid) return View(tc);
        await repo.InsertAsync(tc);
        TempData["Success"] = "Đã thêm giao dịch.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var tc = await repo.GetByIdAsync(id);
        if (tc == null) return NotFound();
        return View(tc);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ThuChi tc)
    {
        if (!ModelState.IsValid) return View(tc);
        await repo.UpdateAsync(tc);
        TempData["Success"] = "Đã cập nhật giao dịch.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await repo.DeleteAsync(id);
        TempData["Success"] = "Đã xóa giao dịch.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> XuatExcel(int nam, int thang)
    {
        var ds = await repo.GetAllAsync(nam, thang, null);
        var (tongThu, tongChi) = await repo.GetTongTheoThangAsync(thang, nam);
        var bytes = excel.XuatThuChi(ds, thang, nam, tongThu, tongChi);
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"ThuChi_T{thang}_{nam}.xlsx");
    }
}
