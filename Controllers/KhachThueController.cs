using Microsoft.AspNetCore.Mvc;
using QuanLyNhaTro.Models;
using QuanLyNhaTro.Repositories;
using QuanLyNhaTro.Services;

namespace QuanLyNhaTro.Controllers;

public class KhachThueController(
    KhachThueRepository khachThueRepo,
    HopDongKhachThueRepository cuTruRepo,
    PhongRepository phongRepo,
    KhachThueService khachThueService,
    IWebHostEnvironment environment) : Controller
{
    private const long MaxImageSize = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedImageExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };
    // GET /KhachThue
    public async Task<IActionResult> Index(string? tuKhoa, string? trangThai, int? phongId)
    {
        ViewData["ActiveMenu"] = "khachthue";
        ViewBag.TuKhoa = tuKhoa;
        ViewBag.TrangThai = trangThai;
        ViewBag.PhongId = phongId;
        ViewBag.DanhSachPhong = await phongRepo.GetAllAsync();
        return View(await khachThueRepo.GetForIndexAsync(tuKhoa, trangThai, phongId));
    }

    [HttpGet]
    public async Task<IActionResult> Search(string? term, int limit = 20)
    {
        var rows = await khachThueRepo.SearchAsync(term, limit);
        return Json(rows.Select(x => new
        {
            id = x.Id,
            hoTen = x.HoTen,
            soDienThoai = x.SoDienThoai,
            cccd = x.CCCD,
            bienSoXe = x.BienSoXe
        }));
    }

    // GET /KhachThue/Details/5
    public async Task<IActionResult> Details(int id)
    {
        ViewData["ActiveMenu"] = "khachthue";
        var khach = await khachThueRepo.GetByIdAsync(id);
        if (khach == null) return NotFound();
        ViewBag.LichSuCuTru = await cuTruRepo.GetByKhachThueAsync(id);
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

        var trungCccd = await khachThueService.TimHoSoTrungCccdAsync(khach.CCCD);
        if (trungCccd != null)
        {
            ViewBag.HoSoTrungCccd = trungCccd;
            ModelState.AddModelError(nameof(KhachThue.CCCD),
                "CCCD đã thuộc một hồ sơ cũ. Hãy mở hồ sơ đó và tái sử dụng thay vì tạo mới.");
            return View(khach);
        }

        if (!await TryLuuAnhAsync(khach, anhCCCDMatTruoc, anhCCCDMatSau))
            return View(khach);

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

        var trungCccd = await khachThueService.TimHoSoTrungCccdAsync(khach.CCCD, id);
        if (trungCccd != null)
        {
            ViewBag.HoSoTrungCccd = trungCccd;
            ModelState.AddModelError(nameof(KhachThue.CCCD),
                "CCCD đã thuộc một hồ sơ khác. Hãy dùng hồ sơ cũ; hệ thống không tự động gộp hồ sơ.");
            return View(khach);
        }

        if (!await TryLuuAnhAsync(khach, anhCCCDMatTruoc, anhCCCDMatSau))
            return View(khach);

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
    private async Task<bool> TryLuuAnhAsync(
        KhachThue khach,
        IFormFile? anhCCCDMatTruoc,
        IFormFile? anhCCCDMatSau)
    {
        if (!ValidateImage(anhCCCDMatTruoc, "Ảnh CCCD mặt trước")
            || !ValidateImage(anhCCCDMatSau, "Ảnh CCCD mặt sau"))
            return false;

        try
        {
            if (anhCCCDMatTruoc is { Length: > 0 })
                khach.AnhCCCDMatTruoc = await LuuAnhAsync(anhCCCDMatTruoc);
            if (anhCCCDMatSau is { Length: > 0 })
                khach.AnhCCCDMatSau = await LuuAnhAsync(anhCCCDMatSau);
            return true;
        }
        catch (IOException)
        {
            ModelState.AddModelError(string.Empty, "Không thể lưu ảnh CCCD. Vui lòng kiểm tra quyền ghi thư mục uploads hoặc thử lại.");
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            ModelState.AddModelError(string.Empty, "Ứng dụng không có quyền ghi ảnh vào thư mục uploads.");
            return false;
        }
    }

    private bool ValidateImage(IFormFile? file, string label)
    {
        if (file == null || file.Length == 0) return true;
        if (file.Length > MaxImageSize)
        {
            ModelState.AddModelError(string.Empty, $"{label} không được vượt quá 5 MB.");
            return false;
        }

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedImageExtensions.Contains(extension))
        {
            ModelState.AddModelError(string.Empty, $"{label} chỉ chấp nhận JPG, PNG hoặc WEBP.");
            return false;
        }
        return true;
    }

    private async Task<string> LuuAnhAsync(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var uploadDirectory = Path.Combine(environment.WebRootPath, "uploads");
        Directory.CreateDirectory(uploadDirectory);
        var path = Path.Combine(uploadDirectory, fileName);
        await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await file.CopyToAsync(stream);
        return $"/uploads/{fileName}";
    }
}
