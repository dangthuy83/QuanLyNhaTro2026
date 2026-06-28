using Microsoft.AspNetCore.Mvc;
using QuanLyNhaTro.Models;
using QuanLyNhaTro.Repositories;

namespace QuanLyNhaTro.Controllers;

public class PhongController(
    PhongRepository phongRepo,
    NhaRepository nhaRepo,
    HopDongRepository hopDongRepo,
    PhongDichVuRepository phongDichVuRepo,
    DichVuRepository dichVuRepo) : Controller
{
    // GET /Phong
    public async Task<IActionResult> Index()
    {
        ViewData["ActiveMenu"] = "phong";
        var danhSach = await phongRepo.GetAllAsync();
        return View(danhSach);
    }

    // GET /Phong/Details/5
    public async Task<IActionResult> Details(int id)
    {
        ViewData["ActiveMenu"] = "phong";
        var phong = await phongRepo.GetByIdAsync(id);
        if (phong == null) return NotFound();

        var hopDong = await hopDongRepo.GetDangHieuLucByPhongAsync(id);
        var dichVuPhong = await phongDichVuRepo.GetByPhongAsync(id);

        ViewBag.HopDong = hopDong;
        ViewBag.DichVuPhong = dichVuPhong;
        return View(phong);
    }

    // GET /Phong/Create
    public async Task<IActionResult> Create()
    {
        ViewData["ActiveMenu"] = "phong";
        await LoadPhongFormDataAsync();
        return View(new Phong());
    }

    // POST /Phong/Create
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Phong phong, int[] dichVuIds, decimal[] donGias)
    {
        await ValidateNhaAsync(phong.NhaId);

        if (!ModelState.IsValid)
        {
            await LoadPhongFormDataAsync();
            return View(phong);
        }

        phong.TrangThai = "Trong";
        var phongId = await phongRepo.InsertAsync(phong);

        // Gắn dịch vụ cho phòng
        for (int i = 0; i < dichVuIds.Length; i++)
        {
            await phongDichVuRepo.InsertAsync(new PhongDichVu
            {
                PhongId  = phongId,
                DichVuId = dichVuIds[i],
                DonGia   = i < donGias.Length ? donGias[i] : 0,
                DangApDung = true
            });
        }

        TempData["Success"] = "Đã thêm phòng thành công.";
        return RedirectToAction(nameof(Index));
    }

    // GET /Phong/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        ViewData["ActiveMenu"] = "phong";
        var phong = await phongRepo.GetByIdAsync(id);
        if (phong == null) return NotFound();

        await LoadPhongFormDataAsync(id);
        return View(phong);
    }

    // POST /Phong/Edit/5
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Phong phong)
    {
        if (id != phong.Id) return BadRequest();
        await ValidateNhaAsync(phong.NhaId);

        if (!ModelState.IsValid)
        {
            await LoadPhongFormDataAsync(id);
            return View(phong);
        }

        await phongRepo.UpdateAsync(phong);
        TempData["Success"] = "Đã cập nhật phòng.";
        return RedirectToAction(nameof(Index));
    }

    // POST /Phong/Delete/5
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await phongRepo.DeleteAsync(id);
        TempData["Success"] = "Đã xoá phòng.";
        return RedirectToAction(nameof(Index));
    }

    // POST /Phong/CapNhatDonGia  — AJAX: cập nhật đơn giá 1 dịch vụ
    [HttpPost]
    public async Task<IActionResult> CapNhatDonGia(int phongDichVuId, decimal donGia)
    {
        await phongDichVuRepo.UpdateDonGiaAsync(phongDichVuId, donGia);
        return Ok();
    }

    private async Task LoadPhongFormDataAsync(int? phongId = null)
    {
        ViewBag.DanhSachNha = await nhaRepo.GetAllAsync();
        ViewBag.DanhSachDichVu = await dichVuRepo.GetAllAsync(hienThi: true);

        if (phongId.HasValue)
        {
            ViewBag.DichVuPhong = await phongDichVuRepo.GetByPhongAsync(phongId.Value);
        }
    }

    private async Task ValidateNhaAsync(int nhaId)
    {
        if (nhaId <= 0)
        {
            ModelState.AddModelError(nameof(Phong.NhaId), "Vui long chon nha.");
            return;
        }

        var nha = await nhaRepo.GetByIdAsync(nhaId);
        if (nha == null)
        {
            ModelState.AddModelError(nameof(Phong.NhaId), "Nha da chon khong ton tai.");
        }
    }
}
