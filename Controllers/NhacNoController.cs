using Microsoft.AspNetCore.Mvc;
using QuanLyNhaTro.Models;
using QuanLyNhaTro.Repositories;

namespace QuanLyNhaTro.Controllers;

public class NhacNoController(
    HoaDonRepository hoaDonRepo,
    NhaRepository nhaRepo) : Controller
{
    public async Task<IActionResult> Index(
        int? nhaId,
        string? trangThaiHopDong,
        string? nhomNo,
        string? tuKhoa)
    {
        ViewData["ActiveMenu"] = "nhacno";

        trangThaiHopDong ??= "DangHieuLuc";
        nhomNo ??= "QuaHan";

        var all = (await hoaDonRepo.GetCongNoAsync()).ToList();
        var ds = ApplyFilters(all, nhaId, trangThaiHopDong, nhomNo, tuKhoa);

        ViewBag.DanhSachNha = await nhaRepo.GetAllAsync();
        ViewBag.NhaId = nhaId;
        ViewBag.TrangThaiHopDong = trangThaiHopDong;
        ViewBag.NhomNo = nhomNo;
        ViewBag.TuKhoa = tuKhoa ?? "";

        ViewBag.TongNoTheoBoLoc = ds.Sum(x => x.ConLai);
        ViewBag.SoHoaDonTheoBoLoc = ds.Count;
        ViewBag.TongNoQuaHan = all.Where(x => x.SoNgayQuaHan > 0).Sum(x => x.ConLai);
        ViewBag.SoHoaDonQuaHan = all.Count(x => x.SoNgayQuaHan > 0);
        ViewBag.TongNoDangOQuaHan = all
            .Where(x => x.DangOHienTai && x.SoNgayQuaHan > 0)
            .Sum(x => x.ConLai);
        ViewBag.SoHoaDonQuaHan30 = all.Count(x => x.SoNgayQuaHan > 30);

        return View(ds);
    }

    private static List<BaoCaoCongNoViewModel> ApplyFilters(
        IEnumerable<BaoCaoCongNoViewModel> source,
        int? nhaId,
        string? trangThaiHopDong,
        string? nhomNo,
        string? tuKhoa)
    {
        var query = source.AsEnumerable();

        if (nhaId.HasValue)
            query = query.Where(x => x.NhaId == nhaId.Value);

        if (!string.IsNullOrWhiteSpace(trangThaiHopDong))
            query = query.Where(x => x.TrangThaiHopDong == trangThaiHopDong);

        query = nhomNo switch
        {
            "TatCaNo" => query,
            "QuaHan30" => query.Where(x => x.SoNgayQuaHan > 30),
            "ChuaQuaHan" => query.Where(x => x.SoNgayQuaHan == 0),
            _ => query.Where(x => x.SoNgayQuaHan > 0)
        };

        if (!string.IsNullOrWhiteSpace(tuKhoa))
        {
            var keyword = tuKhoa.Trim();
            query = query.Where(x =>
                x.TenNha.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                x.TenPhong.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                x.TenKhachChinh.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                (x.SoDienThoai?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        return query
            .OrderByDescending(x => x.SoNgayQuaHan)
            .ThenBy(x => x.TenNha)
            .ThenBy(x => x.TenPhong)
            .ThenBy(x => x.Nam)
            .ThenBy(x => x.Thang)
            .ToList();
    }
}
