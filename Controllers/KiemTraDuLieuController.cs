using Microsoft.AspNetCore.Mvc;
using QuanLyNhaTro.Models;
using QuanLyNhaTro.Repositories;
using QuanLyNhaTro.Services;

namespace QuanLyNhaTro.Controllers;

public class KiemTraDuLieuController(
    KiemTraDuLieuRepository kiemTraRepo,
    NhaRepository nhaRepo,
    HoaDonService hoaDonService) : Controller
{
    public async Task<IActionResult> Index(
        int? thang,
        int? nam,
        int? nhaId,
        string? tuKhoa,
        string trangThaiDong = "TatCa")
    {
        ViewData["ActiveMenu"] = "kiemtra";
        var ky = DefaultBillingPeriodResolver.Resolve(thang, nam);
        thang = ky.Thang;
        nam = ky.Nam;
        tuKhoa = tuKhoa?.Trim();
        trangThaiDong = NormalizeTrangThaiDong(trangThaiDong);

        var rows = (await kiemTraRepo.GetRowsAsync(thang.Value, nam.Value, nhaId)).ToList();
        foreach (var row in rows)
        {
            row.DuKien = await hoaDonService.TinhHoaDonDuKienAsync(row.HopDongId, thang.Value, nam.Value);
        }

        rows = rows
            .Where(row => MatchesTuKhoa(row, tuKhoa))
            .Where(row => MatchesTrangThaiDong(row, trangThaiDong))
            .ToList();

        var model = new KiemTraDuLieuViewModel
        {
            Thang = thang.Value,
            Nam = nam.Value,
            NhaId = nhaId,
            TuKhoa = tuKhoa,
            TrangThaiDong = trangThaiDong,
            DanhSachNha = (await nhaRepo.GetAllAsync()).ToList(),
            Rows = rows,
            CanhBaoDoiSoat = (await kiemTraRepo.GetReconcileIssuesAsync()).ToList()
        };

        return View(model);
    }

    private static string NormalizeTrangThaiDong(string? trangThaiDong)
        => trangThaiDong is "SanSang" or "CanXuLy" or "ThieuKhach" or "ThieuDichVu"
            or "ThieuDonGia" or "ThieuChiSo" or "DaCoHoaDon"
            ? trangThaiDong
            : "TatCa";

    private static bool MatchesTrangThaiDong(KiemTraDuLieuRow row, string trangThaiDong)
        => trangThaiDong switch
        {
            "SanSang" => row.SanSangVanHanh,
            "CanXuLy" => row.CanXuLy,
            "ThieuKhach" => row.ThieuKhach,
            "ThieuDichVu" => row.ThieuDichVu,
            "ThieuDonGia" => row.ThieuDonGia,
            "ThieuChiSo" => row.ThieuChiSo,
            "DaCoHoaDon" => row.DaCoHoaDon,
            _ => true
        };

    private static bool MatchesTuKhoa(KiemTraDuLieuRow row, string? tuKhoa)
    {
        if (string.IsNullOrWhiteSpace(tuKhoa))
            return true;

        var keyword = tuKhoa.Trim();
        return row.HopDongId.ToString().Contains(keyword, StringComparison.OrdinalIgnoreCase)
            || row.TenNha.Contains(keyword, StringComparison.OrdinalIgnoreCase)
            || row.TenPhong.Contains(keyword, StringComparison.OrdinalIgnoreCase)
            || row.TenKhach?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true;
    }
}
