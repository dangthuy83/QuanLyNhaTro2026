using Microsoft.AspNetCore.Mvc;
using QuanLyNhaTro.Models;
using QuanLyNhaTro.Repositories;
using QuanLyNhaTro.Services;

namespace QuanLyNhaTro.Controllers;

public class ChiSoNgoaiHopDongController(
    ChiSoNgoaiHopDongRepository chiSoNgoaiHopDongRepo,
    ChiSoNgoaiHopDongService chiSoNgoaiHopDongService,
    MeterContinuityService continuity,
    PhongRepository phongRepo,
    DichVuRepository dichVuRepo) : Controller
{
    public async Task<IActionResult> Index(int? phongId, int? dichVuId)
    {
        ViewData["ActiveMenu"] = "chiso-ngoai";
        await LoadFormDataAsync(phongId, dichVuId);
        return View(await chiSoNgoaiHopDongRepo.GetAllAsync(phongId, dichVuId));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ChiSoNgoaiHopDong model)
    {
        try
        {
            await chiSoNgoaiHopDongService.CreateAsync(model);
            TempData["Success"] = "Da ghi nhan chi so ngoai hop dong.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = "Khong the luu chi so ngoai hop dong. " + ex.Message;
        }
        return RedirectToAction(nameof(Index), new { phongId = model.PhongId, dichVuId = model.DichVuId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await chiSoNgoaiHopDongRepo.GetByIdAsync(id);
        if (existing == null) return NotFound();

        try
        {
            await chiSoNgoaiHopDongService.DeleteAsync(id);
            TempData["Success"] = "Da xoa chi so ngoai hop dong.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index), new { phongId = existing.PhongId, dichVuId = existing.DichVuId });
    }

    private async Task LoadFormDataAsync(int? phongId, int? dichVuId)
    {
        ViewBag.PhongId = phongId;
        ViewBag.DichVuId = dichVuId;
        ViewBag.DanhSachPhong = (await phongRepo.GetAllAsync()).ToList();
        ViewBag.DichVuTheoChiSo = (await dichVuRepo.GetAllAsync())
            .Where(dv => dv.LoaiTinhPhi == DichVu.LoaiTheoChiSo)
            .ToList();

        if (phongId.HasValue && dichVuId.HasValue)
        {
            var latest = await continuity.GetLatestAsync(phongId.Value, dichVuId.Value);
            ViewBag.GoiYTuChiSo = latest?.EndReading;
            ViewBag.NguonGoiYTuChiSo = latest?.Description;
        }
    }
}
