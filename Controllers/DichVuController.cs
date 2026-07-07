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
        Normalize(dv);
        await dichVuRepo.InsertAsync(dv);
        TempData["Success"] = "Đã thêm dịch vụ.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(DichVu dv)
    {
        Normalize(dv);
        await dichVuRepo.UpdateAsync(dv);
        TempData["Success"] = "Đã cập nhật.";
        return RedirectToAction(nameof(Index));
    }

    private static void Normalize(DichVu dv)
    {
        if (dv.LoaiTinhPhi is not (DichVu.LoaiCoDinh or DichVu.LoaiTheoChiSo))
            dv.LoaiTinhPhi = DichVu.LoaiCoDinh;

        if (dv.CachTinhCoDinh is not (DichVu.CachTinhTheoPhong or DichVu.CachTinhTheoNguoi))
            dv.CachTinhCoDinh = DichVu.CachTinhTheoPhong;

        if (dv.LoaiTinhPhi == DichVu.LoaiTheoChiSo)
            dv.CachTinhCoDinh = DichVu.CachTinhTheoPhong;
    }
}
