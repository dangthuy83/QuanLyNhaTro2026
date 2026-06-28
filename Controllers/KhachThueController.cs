using Microsoft.AspNetCore.Mvc;
using QuanLyNhaTro.Models;
using QuanLyNhaTro.Repositories;

namespace QuanLyNhaTro.Controllers;

public class KhachThueController(KhachThueRepository khachThueRepo) : Controller
{
    // GET /KhachThue
    public async Task<IActionResult> Index()
    {
        ViewData["ActiveMenu"] = "khachthue";
        return View(await khachThueRepo.GetAllAsync());
    }

    // GET /KhachThue/Details/5
    public async Task<IActionResult> Details(int id)
    {
        ViewData["ActiveMenu"] = "khachthue";
        var khach = await khachThueRepo.GetByIdAsync(id);
        if (khach == null) return NotFound();
        return View(khach);
    }

    // GET /KhachThue/Create
    public IActionResult Create()
    {
        ViewData["ActiveMenu"] = "khachthue";
        return View();
    }

    // POST /KhachThue/Create
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(KhachThue khach, IFormFile? anhCCCDMatTruoc, IFormFile? anhCCCDMatSau)
    {
        if (!ModelState.IsValid) return View(khach);

        if (anhCCCDMatTruoc != null && anhCCCDMatTruoc.Length > 0)
            khach.AnhCCCDMatTruoc = await LuuAnhAsync(anhCCCDMatTruoc);
        if (anhCCCDMatSau != null && anhCCCDMatSau.Length > 0)
            khach.AnhCCCDMatSau = await LuuAnhAsync(anhCCCDMatSau);

        await khachThueRepo.InsertAsync(khach);
        TempData["Success"] = "Đã thêm khách thuê.";
        return RedirectToAction(nameof(Index));
    }

    // GET /KhachThue/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        ViewData["ActiveMenu"] = "khachthue";
        var khach = await khachThueRepo.GetByIdAsync(id);
        if (khach == null) return NotFound();
        return View(khach);
    }

    // POST /KhachThue/Edit/5
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, KhachThue khach, IFormFile? anhCCCDMatTruoc, IFormFile? anhCCCDMatSau)
    {
        if (id != khach.Id) return BadRequest();
        if (!ModelState.IsValid) return View(khach);

        if (anhCCCDMatTruoc != null && anhCCCDMatTruoc.Length > 0)
            khach.AnhCCCDMatTruoc = await LuuAnhAsync(anhCCCDMatTruoc);
        if (anhCCCDMatSau != null && anhCCCDMatSau.Length > 0)
            khach.AnhCCCDMatSau = await LuuAnhAsync(anhCCCDMatSau);

        await khachThueRepo.UpdateAsync(khach);
        TempData["Success"] = "Đã cập nhật thông tin khách thuê.";
        return RedirectToAction(nameof(Index));
    }

    // POST /KhachThue/Delete/5
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await khachThueRepo.DeleteAsync(id);
        TempData["Success"] = "Đã xoá khách thuê.";
        return RedirectToAction(nameof(Index));
    }

    // ── Helper ──────────────────────────────────────────────────────────────
    private async Task<string> LuuAnhAsync(IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid()}{ext}";
        var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", fileName);
        await using var stream = new FileStream(path, FileMode.Create);
        await file.CopyToAsync(stream);
        return $"/uploads/{fileName}";
    }
}
