using Microsoft.AspNetCore.Mvc;
using QuanLyNhaTro.Models;
using QuanLyNhaTro.Repositories;

namespace QuanLyNhaTro.Controllers;

public class DichVuController(DichVuRepository dichVuRepo) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewData["ActiveMenu"] = "dichvu";
        return View(await dichVuRepo.GetAllAsync());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DichVu dv)
    {
        await dichVuRepo.InsertAsync(dv);
        TempData["Success"] = "Đã thêm dịch vụ.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(DichVu dv)
    {
        await dichVuRepo.UpdateAsync(dv);
        TempData["Success"] = "Đã cập nhật.";
        return RedirectToAction(nameof(Index));
    }
}
