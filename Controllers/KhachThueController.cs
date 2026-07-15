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
    TenantPhotoStorage tenantPhotoStorage) : Controller
{
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

    public async Task<IActionResult> Details(int id)
    {
        ViewData["ActiveMenu"] = "khachthue";
        var khach = await khachThueRepo.GetByIdAsync(id);
        if (khach == null) return NotFound();
        ViewBag.LichSuCuTru = await cuTruRepo.GetByKhachThueAsync(id);
        return View(khach);
    }

    [HttpGet]
    public async Task<IActionResult> Photo(int id, string side)
    {
        var khach = await khachThueRepo.GetByIdAsync(id);
        if (khach == null) return NotFound();

        var token = side.ToLowerInvariant() switch
        {
            "front" => khach.AnhCCCDMatTruoc,
            "back" => khach.AnhCCCDMatSau,
            _ => null
        };
        var photo = await tenantPhotoStorage.OpenReadAsync(token);
        if (photo == null) return NotFound();
        Response.Headers.CacheControl = "no-store, private";
        return File(photo.Stream, photo.ContentType);
    }

    public IActionResult Create()
    {
        ViewData["ActiveMenu"] = "khachthue";
        return View(new KhachThue());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        KhachThue khach,
        IFormFile? anhCCCDMatTruoc,
        IFormFile? anhCCCDMatSau,
        bool confirmDuplicatePhone = false,
        CancellationToken cancellationToken = default)
    {
        khach.AnhCCCDMatTruoc = null;
        khach.AnhCCCDMatSau = null;
        if (!ModelState.IsValid) return View(khach);

        try
        {
            var id = await khachThueService.CreateAsync(
                khach, anhCCCDMatTruoc, anhCCCDMatSau, confirmDuplicatePhone, cancellationToken);
            TempData["Success"] = "Đã thêm khách thuê.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (DuplicateCccdException ex)
        {
            ShowDuplicateCccd(ex.ExistingProfile);
        }
        catch (DuplicatePhoneConfirmationException ex)
        {
            ShowDuplicatePhones(ex.MatchingProfiles);
        }
        catch (TenantPhotoValidationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ModelState.AddModelError(string.Empty, "Không thể lưu ảnh CCCD. Vui lòng kiểm tra thư mục upload hoặc thử lại.");
        }
        return View(khach);
    }

    public async Task<IActionResult> Edit(int id)
    {
        ViewData["ActiveMenu"] = "khachthue";
        var khach = await khachThueRepo.GetByIdAsync(id);
        return khach == null ? NotFound() : View(khach);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        KhachThue khach,
        IFormFile? anhCCCDMatTruoc,
        IFormFile? anhCCCDMatSau,
        bool confirmDuplicatePhone = false,
        CancellationToken cancellationToken = default)
    {
        if (id != khach.Id) return BadRequest();
        if (!ModelState.IsValid)
            return await RenderEditWithTrustedPhotosAsync(id, khach);

        try
        {
            await khachThueService.UpdateAsync(
                id, khach, anhCCCDMatTruoc, anhCCCDMatSau, confirmDuplicatePhone, cancellationToken);
            TempData["Success"] = "Đã cập nhật thông tin khách thuê.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (DuplicateCccdException ex)
        {
            ShowDuplicateCccd(ex.ExistingProfile);
        }
        catch (DuplicatePhoneConfirmationException ex)
        {
            ShowDuplicatePhones(ex.MatchingProfiles);
        }
        catch (TenantPhotoValidationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ModelState.AddModelError(string.Empty, "Không thể lưu ảnh CCCD. Ảnh cũ vẫn được giữ nguyên; vui lòng thử lại.");
        }
        return await RenderEditWithTrustedPhotosAsync(id, khach);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await khachThueService.DeleteAsync(id, cancellationToken);
            TempData["Success"] = "Đã xóa hồ sơ khách thuê chưa sử dụng.";
            if (!result.PhotoCleanupComplete)
                TempData["Warning"] = "Hồ sơ đã xóa nhưng có ảnh không thể dọn an toàn; hệ thống không xóa file ngoài thư mục upload.";
        }
        catch (TenantInUseException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> RenderEditWithTrustedPhotosAsync(int id, KhachThue submitted)
    {
        var trusted = await khachThueRepo.GetByIdAsync(id);
        if (trusted == null) return NotFound();
        submitted.AnhCCCDMatTruoc = trusted.AnhCCCDMatTruoc;
        submitted.AnhCCCDMatSau = trusted.AnhCCCDMatSau;
        ViewData["ActiveMenu"] = "khachthue";
        return View("Edit", submitted);
    }

    private void ShowDuplicateCccd(KhachThue profile)
    {
        ViewBag.HoSoTrungCccd = profile;
        ModelState.AddModelError(nameof(KhachThue.CCCD),
            "CCCD đã thuộc một hồ sơ cũ. Hãy mở hồ sơ đó và tái sử dụng; hệ thống không tự động gộp hồ sơ.");
    }

    private void ShowDuplicatePhones(IReadOnlyList<KhachThue> profiles)
    {
        ViewBag.HoSoTrungSoDienThoai = profiles;
        ViewBag.RequirePhoneConfirmation = true;
        ModelState.AddModelError(nameof(KhachThue.SoDienThoai),
            "Số điện thoại đang xuất hiện ở hồ sơ khác. Kiểm tra danh sách bên dưới trước khi lưu hồ sơ riêng.");
    }
}
