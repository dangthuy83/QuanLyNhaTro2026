using Microsoft.AspNetCore.Mvc;
using QuanLyNhaTro.Repositories;
using QuanLyNhaTro.Services;

namespace QuanLyNhaTro.Controllers;

public class BaoCaoController(
    HoaDonRepository hoaDonRepo,
    NhaRepository nhaRepo,
    ExcelService excel) : Controller
{
    public async Task<IActionResult> CongNo(
        int? nhaId,
        string? trangThaiHopDong,
        string? quaHan,
        string? tuKhoa)
    {
        var ds = await LayCongNoDaLocAsync(nhaId, trangThaiHopDong, quaHan, tuKhoa);
        ViewBag.DanhSachNha = await nhaRepo.GetAllAsync();
        ViewBag.NhaId = nhaId;
        ViewBag.TrangThaiHopDong = trangThaiHopDong ?? "";
        ViewBag.QuaHan = quaHan ?? "";
        ViewBag.TuKhoa = tuKhoa ?? "";
        ViewBag.TongNo        = ds.Sum(x => x.ConLai);
        ViewBag.TongNoHienTai = ds.Where(x => x.DangOHienTai).Sum(x => x.ConLai);
        ViewBag.TongNoCu      = ds.Where(x => !x.DangOHienTai).Sum(x => x.ConLai);
        ViewBag.SoHoaDonNo    = ds.Count;
        return View(ds);
    }

    public async Task<IActionResult> XuatCongNo(
        int? nhaId,
        string? trangThaiHopDong,
        string? quaHan,
        string? tuKhoa)
    {
        var ds = await LayCongNoDaLocAsync(nhaId, trangThaiHopDong, quaHan, tuKhoa);
        var bytes = excel.XuatExcelCongNo(ds);
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"CongNo_{DateTime.Today:yyyyMMdd}.xlsx");
    }

    private async Task<List<QuanLyNhaTro.Models.BaoCaoCongNoViewModel>> LayCongNoDaLocAsync(
        int? nhaId,
        string? trangThaiHopDong,
        string? quaHan,
        string? tuKhoa)
    {
        var query = (await hoaDonRepo.GetCongNoAsync()).AsEnumerable();

        if (nhaId.HasValue)
            query = query.Where(x => x.NhaId == nhaId.Value);

        if (!string.IsNullOrWhiteSpace(trangThaiHopDong))
            query = query.Where(x => x.TrangThaiHopDong == trangThaiHopDong);

        query = quaHan switch
        {
            "ChuaQuaHan" => query.Where(x => x.SoNgayQuaHan == 0),
            "QuaHan" => query.Where(x => x.SoNgayQuaHan > 0),
            "QuaHan30" => query.Where(x => x.SoNgayQuaHan > 30),
            _ => query
        };

        if (!string.IsNullOrWhiteSpace(tuKhoa))
        {
            string keyword = tuKhoa.Trim();
            query = query.Where(x =>
                x.TenNha.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                x.TenPhong.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                x.TenKhachChinh.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                (x.SoDienThoai?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        return query.ToList();
    }
}
