using Microsoft.AspNetCore.Mvc;
using QuanLyNhaTro.Models;
using QuanLyNhaTro.Repositories;
using QuanLyNhaTro.Services;

namespace QuanLyNhaTro.Controllers;

public class GiaoDichCocController(
    HopDongRepository hopDongRepo,
    HoaDonRepository hoaDonRepo,
    GiaoDichCocRepository giaoDichCocRepo,
    GiaoDichCocService giaoDichCocService) : Controller
{
    public async Task<IActionResult> Index(int hopDongId)
    {
        ViewData["ActiveMenu"] = "hopdong";

        var hopDong = await hopDongRepo.GetByIdAsync(hopDongId);
        if (hopDong == null) return NotFound();

        var hoaDonConNo = (await hoaDonRepo.GetByHopDongAsync(hopDongId))
            .Where(x => x.TongCong > x.SoTienDaThu)
            .OrderBy(x => x.Nam)
            .ThenBy(x => x.Thang)
            .ToList();

        return View(new GiaoDichCocViewModel
        {
            HopDong = hopDong,
            SoDuCoc = await giaoDichCocService.GetSoDuHienTaiAsync(hopDongId),
            GiaoDich = await giaoDichCocRepo.GetByHopDongAsync(hopDongId),
            HoaDonConNo = hoaDonConNo
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        int hopDongId,
        string loaiGiaoDich,
        decimal soTien,
        DateTime ngayGiaoDich,
        int? hoaDonId,
        string? phuongThuc,
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
                phuongThuc,
                ghiChu);

            TempData["Success"] = "Da ghi nhan giao dich coc.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { hopDongId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> XuLyChenhLech(
        int hopDongId,
        DateTime ngayGiaoDich,
        string phuongThuc,
        string? ghiChu)
    {
        try
        {
            var result = await giaoDichCocService.XuLyChenhLechChuyenPhongAsync(
                hopDongId,
                ngayGiaoDich,
                phuongThuc,
                ghiChu);

            TempData["Success"] = result.LoaiGiaoDich == null
                ? "Chenh lech coc da khop, da danh dau hoan tat."
                : $"Da ghi nhan {result.LoaiGiaoDich} {Math.Abs(result.SoTienChenhLech):N0} d.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { hopDongId });
    }
}
