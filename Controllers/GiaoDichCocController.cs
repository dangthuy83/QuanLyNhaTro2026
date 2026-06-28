using Microsoft.AspNetCore.Mvc;
using QuanLyNhaTro.Models;
using QuanLyNhaTro.Repositories;
using QuanLyNhaTro.Services;

namespace QuanLyNhaTro.Controllers;

public class GiaoDichCocController(
    HopDongRepository hopDongRepo,
    GiaoDichCocRepository giaoDichCocRepo,
    GiaoDichCocService giaoDichCocService) : Controller
{
    public async Task<IActionResult> Index(int hopDongId)
    {
        ViewData["ActiveMenu"] = "hopdong";

        var hopDong = await hopDongRepo.GetByIdAsync(hopDongId);
        if (hopDong == null) return NotFound();

        return View(new GiaoDichCocViewModel
        {
            HopDong = hopDong,
            SoDuCoc = await giaoDichCocService.GetSoDuHienTaiAsync(hopDongId),
            GiaoDich = await giaoDichCocRepo.GetByHopDongAsync(hopDongId)
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        int hopDongId,
        string loaiGiaoDich,
        decimal soTien,
        DateTime ngayGiaoDich,
        int? hoaDonId,
        string? ghiChu)
    {
        try
        {
            await giaoDichCocService.GhiNhanThuCongAsync(
                hopDongId,
                loaiGiaoDich,
                soTien,
                ngayGiaoDich,
                hoaDonId,
                ghiChu);

            TempData["Success"] = "Da ghi nhan giao dich coc.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { hopDongId });
    }
}
