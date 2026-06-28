using Microsoft.AspNetCore.Mvc;
using QuanLyNhaTro.Models;
using QuanLyNhaTro.Repositories;
using QuanLyNhaTro.Services;

namespace QuanLyNhaTro.Controllers;

public class ChuyenPhongController(
    ChuyenPhongService svc,
    HopDongRepository hopDongRepo,
    PhongRepository phongRepo) : Controller
{
    // GET /ChuyenPhong/Create?hopDongId=5
    public async Task<IActionResult> Create(int hopDongId)
    {
        var hd = await hopDongRepo.GetByIdAsync(hopDongId);
        if (hd == null || hd.TrangThai != "DangHieuLuc")
            return BadRequest("Hợp đồng không hợp lệ.");

        var phongCu      = await phongRepo.GetByIdAsync(hd.PhongId);
        var dsPhongTrong = (await phongRepo.GetPhongTrongAsync())
                           .Where(p => p.Id != hd.PhongId);

        return View(new ChuyenPhongViewModel
        {
            HopDongCuId  = hopDongId,
            TenPhongCu   = phongCu?.TenPhong ?? "",
            TienCocCu    = hd.TienCoc,
            TienCocMoi   = hd.TienCoc,
            DsPhongTrong = dsPhongTrong
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ChuyenPhongViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.DsPhongTrong = (await phongRepo.GetPhongTrongAsync())
                              .Where(p => p.Id != vm.HopDongCuId);
            return View(vm);
        }

        try
        {
            var (hdMoiId, _, _) = await svc.ThucHienAsync(vm);
            TempData["Success"] = "Chuyển phòng thành công.";
            return RedirectToAction("Details", "HopDong", new { id = hdMoiId });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            vm.DsPhongTrong = (await phongRepo.GetPhongTrongAsync())
                              .Where(p => p.Id != vm.HopDongCuId);
            return View(vm);
        }
    }
}
