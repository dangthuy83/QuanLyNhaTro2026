using Microsoft.AspNetCore.Mvc;
using QuanLyNhaTro.Models;
using QuanLyNhaTro.Repositories;
using QuanLyNhaTro.Services;

namespace QuanLyNhaTro.Controllers;

public class HopDongController(
    HopDongRepository hopDongRepo,
    HopDongKhachThueRepository hdKhachRepo,
    PhongDichVuRepository phongDichVuRepo,
    HopDongDichVuRepository hopDongDichVuRepo,
    PhongRepository phongRepo,
    KhachThueRepository khachThueRepo,
    HopDongService hopDongService,
    CuTruService cuTruService,
    GiaoDichCocService giaoDichCocService,
    KhoanPhatSinhHopDongRepository khoanPhatSinhRepo) : Controller
{
    public async Task<IActionResult> Index()
    {
        await hopDongService.KichHoatHopDongDenHanAsync(DateTime.Today);
        ViewData["ActiveMenu"] = "hopdong";
        return View(await hopDongRepo.GetAllAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        await hopDongService.KichHoatHopDongDenHanAsync(DateTime.Today);
        ViewData["ActiveMenu"] = "hopdong";
        var hd = await hopDongRepo.GetByIdAsync(id);
        if (hd == null) return NotFound();

        ViewBag.DanhSachKhach = await hdKhachRepo.GetByHopDongAsync(id);
        ViewBag.SoDuCoc = await giaoDichCocService.GetSoDuHienTaiAsync(id);
        ViewBag.KhoanPhatSinh = await khoanPhatSinhRepo.GetByHopDongAsync(id);
        var kyXem = hd.NgayKetThuc ?? DateTime.Today;
        if (kyXem < hd.NgayBatDau) kyXem = hd.NgayBatDau;
        ViewBag.KyDichVu = new DateTime(kyXem.Year, kyXem.Month, 1);
        ViewBag.DichVuHopDong = await hopDongDichVuRepo.GetPhongDichVuByHopDongKyAsync(
            id, kyXem.Month, kyXem.Year);
        return View(hd);
    }

    public async Task<IActionResult> Create(int? phongId)
    {
        ViewData["ActiveMenu"] = "hopdong";
        ViewBag.DanhSachPhong = await phongRepo.GetAllAsync();
        ViewBag.DanhSachKhach = Array.Empty<KhachThue>();
        ViewBag.PhongIdMacDinh = phongId;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        HopDong hd,
        int[] khachThueIds,
        int? khachChinhId,
        int[] phongDichVuIds)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.PhongDichVuIdsDaChon = phongDichVuIds;
            ViewBag.KhachChinhIdDaChon = khachChinhId;
            await NapDuLieuFormCreateAsync(hd.PhongId, khachThueIds);
            return View(hd);
        }

        try
        {
            var hdId = await hopDongService.TaoHopDongAsync(
                hd, khachThueIds, khachChinhId, phongDichVuIds);
            TempData["Success"] = "Đã tạo hợp đồng.";
            return RedirectToAction(nameof(Details), new { id = hdId });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ViewBag.PhongDichVuIdsDaChon = phongDichVuIds;
            ViewBag.KhachChinhIdDaChon = khachChinhId;
            await NapDuLieuFormCreateAsync(hd.PhongId, khachThueIds);
            return View(hd);
        }
    }

    [HttpGet]
    public async Task<IActionResult> DichVuTheoPhong(int phongId)
    {
        var rows = await phongDichVuRepo.GetByPhongAsync(phongId);
        return Json(rows.Select(x => new
        {
            id = x.Id,
            tenDichVu = x.DichVu?.TenDichVu,
            loaiTinhPhi = x.DichVu?.LoaiTinhPhiHienThi,
            cachTinh = x.DichVu?.LoaiTinhPhi == QuanLyNhaTro.Models.DichVu.LoaiCoDinh
                ? x.DichVu.CachTinhCoDinhHienThi
                : null,
            donGia = x.DonGia,
            batBuoc = x.DichVu?.BatBuocKhiThue == true
        }));
    }

    public async Task<IActionResult> DichVu(int id, int? thangApDung, int? namApDung)
    {
        var hopDong = await hopDongRepo.GetByIdAsync(id);
        if (hopDong == null) return NotFound();

        var kyMacDinh = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(1);
        var kyHopDong = new DateTime(hopDong.NgayBatDau.Year, hopDong.NgayBatDau.Month, 1);
        if (kyMacDinh < kyHopDong) kyMacDinh = kyHopDong;
        var thang = thangApDung ?? kyMacDinh.Month;
        var nam = namApDung ?? kyMacDinh.Year;

        return View(await BuildCapNhatDichVuViewModelAsync(hopDong, thang, nam));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DichVu(CapNhatDichVuHopDongViewModel model)
    {
        var hopDong = await hopDongRepo.GetByIdAsync(model.HopDongId);
        if (hopDong == null) return NotFound();

        try
        {
            await hopDongService.CapNhatDichVuAsync(
                model.HopDongId,
                model.PhongDichVuIds,
                model.ThangApDung,
                model.NamApDung);
            TempData["Success"] = $"Da cap nhat dich vu tu ky {model.ThangApDung}/{model.NamApDung}.";
            return RedirectToAction(nameof(Details), new { id = model.HopDongId });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var rebuilt = await BuildCapNhatDichVuViewModelAsync(
                hopDong, model.ThangApDung, model.NamApDung);
            rebuilt.PhongDichVuIds = model.PhongDichVuIds;
            return View(rebuilt);
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        ViewData["ActiveMenu"] = "hopdong";
        var hd = await hopDongRepo.GetByIdAsync(id);
        if (hd == null) return NotFound();

        await NapDuLieuFormEditAsync(id);
        ViewBag.CoDuLieuNghiepVu = await hopDongService.CoDuLieuNghiepVuAsync(id);
        return View(hd);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, HopDong hd, int[] khachThueIds, int? khachChinhId)
    {
        if (id != hd.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            await NapDuLieuFormEditAsync(id);
            ViewBag.CoDuLieuNghiepVu = await hopDongService.CoDuLieuNghiepVuAsync(id);
            return View(hd);
        }

        try
        {
            await hopDongService.SuaHopDongAsync(hd, khachThueIds, khachChinhId);
            TempData["Success"] = "Đã cập nhật hợp đồng.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await NapDuLieuFormEditAsync(id);
            ViewBag.CoDuLieuNghiepVu = await hopDongService.CoDuLieuNghiepVuAsync(id);
            return View(hd);
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> KetThuc(int id)
    {
        var hd = await hopDongRepo.GetByIdAsync(id);
        if (hd == null) return NotFound();

        if (hd.TrangThai != "DangHieuLuc")
        {
            TempData["Error"] = "Hop dong khong con hieu luc.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (DateTime.Today < hd.NgayBatDau.Date)
        {
            TempData["Error"] = "Hợp đồng chưa bắt đầu. Chỉ có thể dùng Hủy nếu chưa phát sinh dữ liệu nghiệp vụ.";
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["Error"] = "Hợp đồng đã bắt đầu phải kết thúc qua flow Trả phòng để quyết toán hóa đơn, chỉ số và cọc.";
        return RedirectToAction("Confirm", "TraPhong", new { hopDongId = id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Huy(int id)
    {
        var hd = await hopDongRepo.GetByIdAsync(id);
        if (hd == null) return NotFound();

        try
        {
            await hopDongService.HuyHopDongAsync(id, DateTime.Today);
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["Success"] = "Đã huỷ hợp đồng.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ThemNguoiO(
        int id,
        int khachThueId,
        DateTime ngayBatDau,
        DateTime? ngayKetThucDuKien,
        bool laDaiDien = false)
    {
        try
        {
            await cuTruService.ThemGiaiDoanAsync(
                id, khachThueId, ngayBatDau, ngayKetThucDuKien, laDaiDien);
            TempData["Success"] = "Đã thêm giai đoạn cư trú mới.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> KetThucCuTru(int id, int cuTruId, DateTime ngayKetThuc)
    {
        try
        {
            await cuTruService.KetThucGiaiDoanAsync(cuTruId, ngayKetThuc);
            TempData["Success"] = "Đã ghi nhận ngày rời thực tế.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task NapDuLieuFormCreateAsync(int? phongIdMacDinh, IEnumerable<int>? selectedKhachIds = null)
    {
        ViewData["ActiveMenu"] = "hopdong";
        ViewBag.DanhSachPhong = await phongRepo.GetAllAsync();
        ViewBag.DanhSachKhach = await khachThueRepo.GetByIdsAsync(selectedKhachIds ?? []);
        ViewBag.PhongIdMacDinh = phongIdMacDinh;
    }

    private async Task NapDuLieuFormEditAsync(int hopDongId)
    {
        ViewData["ActiveMenu"] = "hopdong";
        ViewBag.DanhSachPhong = await phongRepo.GetAllAsync();
        var khachHienTai = (await hdKhachRepo.GetByHopDongAsync(hopDongId)).ToList();
        ViewBag.KhachHienTai = khachHienTai;
        ViewBag.DanhSachKhach = await khachThueRepo.GetByIdsAsync(khachHienTai.Select(x => x.KhachThueId));
    }

    private async Task<CapNhatDichVuHopDongViewModel> BuildCapNhatDichVuViewModelAsync(
        HopDong hopDong,
        int thang,
        int nam)
    {
        if (!BusinessDataLimits.IsValidPeriod(thang, nam))
        {
            thang = DateTime.Today.Month;
            nam = DateTime.Today.Year;
        }

        var selected = await hopDongDichVuRepo.GetPhongDichVuIdsByHopDongKyAsync(
            hopDong.Id, thang, nam);
        var all = (await phongDichVuRepo.GetAllByPhongAsync(hopDong.PhongId))
            .Where(x => x.DangApDung || selected.Contains(x.Id))
            .ToList();
        return new CapNhatDichVuHopDongViewModel
        {
            HopDongId = hopDong.Id,
            TenPhong = hopDong.Phong?.TenPhong ?? $"Phong #{hopDong.PhongId}",
            ThangApDung = thang,
            NamApDung = nam,
            PhongDichVuIds = selected.ToArray(),
            DanhSachDichVu = all
        };
    }
}
