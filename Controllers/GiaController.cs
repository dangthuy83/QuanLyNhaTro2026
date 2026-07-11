using Microsoft.AspNetCore.Mvc;
using QuanLyNhaTro.Models;
using QuanLyNhaTro.Repositories;
using QuanLyNhaTro.Services;

namespace QuanLyNhaTro.Controllers;

public class GiaController(
    LichSuThayDoiGiaRepository lichSuRepo,
    PhongRepository phongRepo,
    PhongDichVuRepository phongDvRepo,
    GiaService giaService) : Controller
{
    // GET /Gia/Phong?phongId=3
    public async Task<IActionResult> Phong(int phongId)
    {
        var phong = await phongRepo.GetByIdAsync(phongId);
        if (phong == null) return NotFound();

        var now = DateTime.Today;
        return View("ThayDoiGia", new ThayDoiGiaViewModel
        {
            LoaiDoiTuong = "Phong",
            DoiTuongId   = phongId,
            TenDoiTuong  = phong.TenPhong,
            GiaHienTai   = phong.GiaThueMacDinh,
            GiaMoi       = phong.GiaThueMacDinh,
            ThangApDung  = now.Month == 12 ? 1 : now.Month + 1,
            NamApDung    = now.Month == 12 ? now.Year + 1 : now.Year,
            LichSu       = await lichSuRepo.GetByDoiTuongAsync("Phong", phongId)
        });
    }

    // GET /Gia/DichVu?phongDichVuId=7
    public async Task<IActionResult> DichVu(int phongDichVuId)
    {
        var pdv = await phongDvRepo.GetByIdAsync(phongDichVuId);
        if (pdv == null) return NotFound();

        var now = DateTime.Today;
        return View("ThayDoiGia", new ThayDoiGiaViewModel
        {
            LoaiDoiTuong = "DichVu",
            DoiTuongId   = phongDichVuId,
            TenDoiTuong  = $"{pdv.Phong?.TenPhong} — {pdv.DichVu?.TenDichVu}",
            GiaHienTai   = pdv.DonGia,
            GiaMoi       = pdv.DonGia,
            ThangApDung  = now.Month == 12 ? 1 : now.Month + 1,
            NamApDung    = now.Month == 12 ? now.Year + 1 : now.Year,
            LichSu       = await lichSuRepo.GetByDoiTuongAsync("DichVu", phongDichVuId)
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> LuuThayDoi(ThayDoiGiaViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.LichSu = await lichSuRepo.GetByDoiTuongAsync(vm.LoaiDoiTuong, vm.DoiTuongId);
            return View("ThayDoiGia", vm);
        }

        try
        {
            await giaService.LuuThayDoiAsync(vm);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            vm.LichSu = await lichSuRepo.GetByDoiTuongAsync(vm.LoaiDoiTuong, vm.DoiTuongId);
            return View("ThayDoiGia", vm);
        }

        TempData["Success"] = $"Đã lưu. Hiệu lực từ T{vm.ThangApDung}/{vm.NamApDung}.";

        return vm.LoaiDoiTuong == "Phong"
            ? RedirectToAction("Details", "Phong", new { id = vm.DoiTuongId })
            : RedirectToAction("DichVu", new { phongDichVuId = vm.DoiTuongId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Xoa(int id, string loai, int doiTuongId)
    {
        await giaService.XoaThayDoiAsync(id);
        TempData["Success"] = "Đã xóa bản ghi thay đổi giá.";
        return loai == "Phong"
            ? RedirectToAction("Phong", new { phongId = doiTuongId })
            : RedirectToAction("DichVu", new { phongDichVuId = doiTuongId });
    }
}
