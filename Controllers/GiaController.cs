using Microsoft.AspNetCore.Mvc;
using QuanLyNhaTro.Models;
using QuanLyNhaTro.Repositories;
using QuanLyNhaTro.Services;

namespace QuanLyNhaTro.Controllers;

public class GiaController(
    LichSuThayDoiGiaRepository lichSuRepo,
    HopDongRepository hopDongRepo,
    PhongDichVuRepository phongDvRepo,
    GiaService giaService) : Controller
{
    // GET /Gia/HopDong?hopDongId=3
    public async Task<IActionResult> HopDong(int hopDongId)
    {
        var hopDong = await hopDongRepo.GetByIdAsync(hopDongId);
        if (hopDong == null) return NotFound();

        var now = DateTime.Today;
        var giaApDung = await lichSuRepo.GetGiaTriApDungAsync(
            "HopDong", hopDongId, now.Month, now.Year) ?? hopDong.TienThueThoaThuan;
        return View("ThayDoiGia", new ThayDoiGiaViewModel
        {
            LoaiDoiTuong = "HopDong",
            DoiTuongId   = hopDongId,
            TenDoiTuong  = $"Hợp đồng #{hopDongId} — {hopDong.Phong?.TenPhong}",
            GiaHienTai   = giaApDung,
            GiaMoi       = giaApDung,
            ThangApDung  = now.Month == 12 ? 1 : now.Month + 1,
            NamApDung    = now.Month == 12 ? now.Year + 1 : now.Year,
            LichSu       = await lichSuRepo.GetByDoiTuongAsync("HopDong", hopDongId)
        });
    }

    // GET /Gia/DichVu?phongDichVuId=7
    public async Task<IActionResult> DichVu(int phongDichVuId)
    {
        var pdv = await phongDvRepo.GetByIdAsync(phongDichVuId);
        if (pdv == null) return NotFound();

        var now = DateTime.Today;
        var giaApDung = await lichSuRepo.GetGiaTriApDungAsync(
            "DichVu", phongDichVuId, now.Month, now.Year) ?? pdv.DonGia;
        return View("ThayDoiGia", new ThayDoiGiaViewModel
        {
            LoaiDoiTuong = "DichVu",
            DoiTuongId   = phongDichVuId,
            TenDoiTuong  = $"{pdv.Phong?.TenPhong} — {pdv.DichVu?.TenDichVu}",
            GiaHienTai   = giaApDung,
            GiaMoi       = giaApDung,
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
            await NapLaiViewModelAsync(vm);
            return View("ThayDoiGia", vm);
        }

        try
        {
            await giaService.LuuThayDoiAsync(vm);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await NapLaiViewModelAsync(vm);
            return View("ThayDoiGia", vm);
        }

        TempData["Success"] = $"Đã lưu. Hiệu lực từ T{vm.ThangApDung}/{vm.NamApDung}.";

        return vm.LoaiDoiTuong == "HopDong"
            ? RedirectToAction("Details", "HopDong", new { id = vm.DoiTuongId })
            : RedirectToAction("DichVu", new { phongDichVuId = vm.DoiTuongId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Xoa(int id, string loai, int doiTuongId)
    {
        try
        {
            var item = await giaService.XoaThayDoiAsync(id);
            TempData["Success"] = "Đã xóa bản ghi thay đổi giá.";
            return item.LoaiDoiTuong == "HopDong"
                ? RedirectToAction("HopDong", new { hopDongId = item.DoiTuongId })
                : RedirectToAction("DichVu", new { phongDichVuId = item.DoiTuongId });
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return loai == "HopDong"
                ? RedirectToAction("HopDong", new { hopDongId = doiTuongId })
                : RedirectToAction("DichVu", new { phongDichVuId = doiTuongId });
        }
    }

    private async Task NapLaiViewModelAsync(ThayDoiGiaViewModel vm)
    {
        var now = DateTime.Today;
        if (vm.LoaiDoiTuong == "HopDong")
        {
            var hopDong = await hopDongRepo.GetByIdAsync(vm.DoiTuongId)
                ?? throw new InvalidOperationException("Không tìm thấy hợp đồng cần thay đổi giá.");
            vm.TenDoiTuong = $"Hợp đồng #{hopDong.Id} — {hopDong.Phong?.TenPhong}";
            vm.GiaHienTai = await lichSuRepo.GetGiaTriApDungAsync(
                "HopDong", hopDong.Id, now.Month, now.Year) ?? hopDong.TienThueThoaThuan;
        }
        else if (vm.LoaiDoiTuong == "DichVu")
        {
            var pdv = await phongDvRepo.GetByIdAsync(vm.DoiTuongId)
                ?? throw new InvalidOperationException("Không tìm thấy dịch vụ phòng cần thay đổi giá.");
            vm.TenDoiTuong = $"{pdv.Phong?.TenPhong} — {pdv.DichVu?.TenDichVu}";
            vm.GiaHienTai = await lichSuRepo.GetGiaTriApDungAsync(
                "DichVu", pdv.Id, now.Month, now.Year) ?? pdv.DonGia;
        }
        else
        {
            throw new InvalidOperationException("Loại đối tượng giá không hợp lệ.");
        }

        vm.LichSu = await lichSuRepo.GetByDoiTuongAsync(vm.LoaiDoiTuong, vm.DoiTuongId);
    }
}
