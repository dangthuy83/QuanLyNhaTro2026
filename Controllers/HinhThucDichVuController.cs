using Dapper;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using QuanLyNhaTro.Models;
using QuanLyNhaTro.Repositories;
using QuanLyNhaTro.Services;

namespace QuanLyNhaTro.Controllers;

public class HinhThucDichVuController(IDbConnection db, DichVuRepository dichVuRepo,
    LichSuHinhThucDichVuRepository lichSuRepo, HinhThucDichVuService service) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Create(int dichVuId)
    {
        var dv = await dichVuRepo.GetByIdAsync(dichVuId);
        if (dv == null) return NotFound();
        var next = DateTime.Today.AddMonths(1);
        var vm = new ThayDoiHinhThucDichVuViewModel { DichVuId=dichVuId, DichVu=dv,
            LoaiTinhPhiMoi=dv.LoaiTinhPhi, CachTinhCoDinhMoi=dv.CachTinhCoDinh,
            ThangApDung=next.Month, NamApDung=next.Year };
        await LoadAsync(vm, false);
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ThayDoiHinhThucDichVuViewModel vm)
    {
        try {
            await service.ApplyForPeriodAsync(vm);
            TempData["Success"] = "Đã ghi nhận thay đổi hình thức dịch vụ theo kỳ.";
            return RedirectToAction(nameof(Create), new { dichVuId=vm.DichVuId });
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException) {
            ModelState.AddModelError(string.Empty, ex.Message);
            vm.DichVu = await dichVuRepo.GetByIdAsync(vm.DichVuId);
            await LoadAsync(vm, true);
            return View(vm);
        }
    }

    private async Task LoadAsync(ThayDoiHinhThucDichVuViewModel vm, bool preserve)
    {
        var entered = vm.PhongLienQuan.ToDictionary(x=>x.PhongId, x=>x.ChiSoDau);
        vm.PhongLienQuan = (await db.QueryAsync<ChiSoDauChuyenDoiRow>("""
            SELECT DISTINCT p.Id AS PhongId,p.TenPhong FROM PhongDichVu pdv
            JOIN Phong p ON p.Id=pdv.PhongId WHERE pdv.DichVuId=@DichVuId ORDER BY p.TenPhong
            """, new { vm.DichVuId })).ToList();
        if (preserve) foreach (var row in vm.PhongLienQuan)
            if (entered.TryGetValue(row.PhongId, out var value)) row.ChiSoDau=value;
        vm.LichSu=(await lichSuRepo.GetByDichVuAsync(vm.DichVuId)).ToList();
    }
}
