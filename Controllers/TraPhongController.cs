using Microsoft.AspNetCore.Mvc;
using QuanLyNhaTro.Repositories;
using QuanLyNhaTro.Services;

namespace QuanLyNhaTro.Controllers;

public class TraPhongController(
    TraPhongService svc,
    HopDongRepository hopDongRepo) : Controller
{
    // GET /TraPhong/Confirm?hopDongId=5&ngayTraPhong=2025-06-15
    public async Task<IActionResult> Confirm(int hopDongId, DateTime? ngayTraPhong)
    {
        var hd = await hopDongRepo.GetByIdAsync(hopDongId);
        if (hd == null || hd.TrangThai != "DangHieuLuc")
            return BadRequest("Hợp đồng không hợp lệ.");

        var vm = await svc.TinhPreviewAsync(hopDongId, ngayTraPhong ?? DateTime.Today);
        if (!vm.CoTheTraPhong && !string.IsNullOrWhiteSpace(vm.LyDoChanTraPhong))
            ModelState.AddModelError(string.Empty, vm.LyDoChanTraPhong);
        return View(vm);
    }

    // POST /TraPhong/Execute
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Execute(int hopDongId, DateTime ngayTraPhong, string? ghiChu)
    {
        var hd = await hopDongRepo.GetByIdAsync(hopDongId);
        if (hd == null || hd.TrangThai != "DangHieuLuc")
            return BadRequest("Hợp đồng không hợp lệ.");

        try
        {
            var ketQua = await svc.ThucHienAsync(hopDongId, ngayTraPhong, ghiChu);
            return View("KetQua", ketQua);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Lỗi: {ex.Message}";
            return RedirectToAction(nameof(Confirm), new { hopDongId, ngayTraPhong });
        }
    }
}
