using Microsoft.AspNetCore.Mvc;
using QuanLyNhaTro.Models;
using QuanLyNhaTro.Repositories;
using QuanLyNhaTro.Services;

namespace QuanLyNhaTro.Controllers;

public class HopDongController(
    HopDongRepository hopDongRepo,
    HopDongKhachThueRepository hdKhachRepo,
    PhongRepository phongRepo,
    KhachThueRepository khachThueRepo,
    HopDongService hopDongService,
    PhongService phongService,
    GiaoDichCocService giaoDichCocService) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewData["ActiveMenu"] = "hopdong";
        return View(await hopDongRepo.GetAllAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        ViewData["ActiveMenu"] = "hopdong";
        var hd = await hopDongRepo.GetByIdAsync(id);
        if (hd == null) return NotFound();

        ViewBag.DanhSachKhach = await hdKhachRepo.GetByHopDongAsync(id);
        ViewBag.SoDuCoc = await giaoDichCocService.GetSoDuHienTaiAsync(id);
        return View(hd);
    }

    public async Task<IActionResult> Create(int? phongId)
    {
        ViewData["ActiveMenu"] = "hopdong";
        ViewBag.DanhSachPhong = await phongRepo.GetAllAsync();
        ViewBag.DanhSachKhach = await khachThueRepo.GetAllAsync();
        ViewBag.PhongIdMacDinh = phongId;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(HopDong hd, int[] khachThueIds, int? khachChinhId)
    {
        if (!ModelState.IsValid)
        {
            await NapDuLieuFormCreateAsync(hd.PhongId);
            return View(hd);
        }

        try
        {
            var hdId = await hopDongService.TaoHopDongAsync(hd, khachThueIds, khachChinhId);
            TempData["Success"] = "Đã tạo hợp đồng.";
            return RedirectToAction(nameof(Details), new { id = hdId });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await NapDuLieuFormCreateAsync(hd.PhongId);
            return View(hd);
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        ViewData["ActiveMenu"] = "hopdong";
        var hd = await hopDongRepo.GetByIdAsync(id);
        if (hd == null) return NotFound();

        await NapDuLieuFormEditAsync(id);
        return View(hd);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, HopDong hd, int[] khachThueIds, int? khachChinhId)
    {
        if (id != hd.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            await NapDuLieuFormEditAsync(id);
            return View(hd);
        }

        try
        {
            await hopDongService.SuaHopDongAsync(hd, khachThueIds, khachChinhId);
            TempData["Success"] = "Đã cập nhật hợp đồng.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await NapDuLieuFormEditAsync(id);
            return View(hd);
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> KetThuc(int id)
    {
        var hd = await hopDongRepo.GetByIdAsync(id);
        if (hd == null) return NotFound();

        await hopDongRepo.UpdateTrangThaiAsync(id, "DaKetThuc");
        await phongService.XuLyKetThucHopDongAsync(hd.PhongId);

        TempData["Success"] = "Đã kết thúc hợp đồng.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Huy(int id)
    {
        var hd = await hopDongRepo.GetByIdAsync(id);
        if (hd == null) return NotFound();

        await hopDongRepo.UpdateTrangThaiAsync(id, "DaHuy");
        await phongService.XuLyKetThucHopDongAsync(hd.PhongId);

        TempData["Success"] = "Đã huỷ hợp đồng.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task NapDuLieuFormCreateAsync(int? phongIdMacDinh)
    {
        ViewData["ActiveMenu"] = "hopdong";
        ViewBag.DanhSachPhong = await phongRepo.GetAllAsync();
        ViewBag.DanhSachKhach = await khachThueRepo.GetAllAsync();
        ViewBag.PhongIdMacDinh = phongIdMacDinh;
    }

    private async Task NapDuLieuFormEditAsync(int hopDongId)
    {
        ViewData["ActiveMenu"] = "hopdong";
        ViewBag.DanhSachPhong = await phongRepo.GetAllAsync();
        ViewBag.DanhSachKhach = await khachThueRepo.GetAllAsync();
        ViewBag.KhachHienTai = await hdKhachRepo.GetByHopDongAsync(hopDongId);
    }
}
