using Microsoft.AspNetCore.Mvc;
using QuanLyNhaTro.Repositories;
using QuanLyNhaTro.Services;

namespace QuanLyNhaTro.Controllers;

public class BaoCaoController(
    HoaDonRepository hoaDonRepo,
    ExcelService excel) : Controller
{
    public async Task<IActionResult> CongNo()
    {
        var ds = (await hoaDonRepo.GetCongNoAsync()).ToList();
        ViewBag.TongNo        = ds.Sum(x => x.ConLai);
        ViewBag.TongNoHienTai = ds.Where(x => x.DangOHienTai).Sum(x => x.ConLai);
        ViewBag.TongNoCu      = ds.Where(x => !x.DangOHienTai).Sum(x => x.ConLai);
        return View(ds);
    }

    public async Task<IActionResult> XuatCongNo()
    {
        var ds = (await hoaDonRepo.GetCongNoAsync()).ToList();
        var bytes = excel.XuatExcelCongNo(ds);
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"CongNo_{DateTime.Today:yyyyMMdd}.xlsx");
    }
}
