using Microsoft.AspNetCore.Mvc;
using QuanLyNhaTro.Models;
using QuanLyNhaTro.Repositories;

namespace QuanLyNhaTro.Controllers;

public class KhoanPhatSinhController(
    KhoanPhatSinhHopDongRepository khoanPhatSinhRepo,
    HopDongRepository hopDongRepo) : Controller
{
    public async Task<IActionResult> Index(int hopDongId)
    {
        ViewData["ActiveMenu"] = "hopdong";
        var hopDong = await hopDongRepo.GetByIdAsync(hopDongId);
        if (hopDong == null) return NotFound();

        ViewBag.HopDong = hopDong;
        return View(await khoanPhatSinhRepo.GetByHopDongAsync(hopDongId));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(KhoanPhatSinhHopDong model)
    {
        var hopDong = await hopDongRepo.GetByIdAsync(model.HopDongId);
        if (hopDong == null) return NotFound();

        if (model.SoTien <= 0)
        {
            TempData["Error"] = "So tien phat sinh phai lon hon 0.";
            return RedirectToAction(nameof(Index), new { hopDongId = model.HopDongId });
        }

        if (string.IsNullOrWhiteSpace(model.MoTa))
        {
            TempData["Error"] = "Vui long nhap mo ta khoan phat sinh.";
            return RedirectToAction(nameof(Index), new { hopDongId = model.HopDongId });
        }

        model.TrangThai = KhoanPhatSinhHopDong.TrangThaiChuaXuLy;
        model.SoTienDaXuLy = 0;
        await khoanPhatSinhRepo.InsertAsync(model);

        TempData["Success"] = "Da ghi nhan khoan phat sinh.";
        return RedirectToAction(nameof(Index), new { hopDongId = model.HopDongId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Huy(int id, int hopDongId)
    {
        await khoanPhatSinhRepo.HuyAsync(id);
        TempData["Success"] = "Da huy khoan phat sinh.";
        return RedirectToAction(nameof(Index), new { hopDongId });
    }
}
