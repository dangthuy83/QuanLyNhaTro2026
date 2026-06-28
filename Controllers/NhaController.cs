using Microsoft.AspNetCore.Mvc;
using QuanLyNhaTro.Models;
using QuanLyNhaTro.Repositories;

namespace QuanLyNhaTro.Controllers;

public class NhaController(NhaRepository nhaRepo) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewData["ActiveMenu"] = "nha";
        return View(await nhaRepo.GetAllAsync());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Nha nha)
    {
        if (string.IsNullOrWhiteSpace(nha.TenNha))
        {
            TempData["Error"] = "Ten nha la bat buoc.";
            return RedirectToAction(nameof(Index));
        }

        nha.TenNha = nha.TenNha.Trim();
        await nhaRepo.InsertAsync(nha);
        TempData["Success"] = "Da them nha.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Nha nha)
    {
        if (nha.Id <= 0) return BadRequest();
        if (string.IsNullOrWhiteSpace(nha.TenNha))
        {
            TempData["Error"] = "Ten nha la bat buoc.";
            return RedirectToAction(nameof(Index));
        }

        var existing = await nhaRepo.GetByIdAsync(nha.Id);
        if (existing == null) return NotFound();

        nha.TenNha = nha.TenNha.Trim();
        await nhaRepo.UpdateAsync(nha);
        TempData["Success"] = "Da cap nhat nha.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await nhaRepo.GetByIdAsync(id);
        if (existing == null) return NotFound();

        var soPhong = await nhaRepo.CountPhongAsync(id);
        if (soPhong > 0)
        {
            TempData["Error"] = "Khong the xoa nha dang co phong. Hay chuyen/xoa phong truoc.";
            return RedirectToAction(nameof(Index));
        }

        await nhaRepo.DeleteAsync(id);
        TempData["Success"] = "Da xoa nha.";
        return RedirectToAction(nameof(Index));
    }
}
