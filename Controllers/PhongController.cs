using Microsoft.AspNetCore.Mvc;
using QuanLyNhaTro.Models;
using QuanLyNhaTro.Repositories;
using QuanLyNhaTro.Services;

namespace QuanLyNhaTro.Controllers;

public class PhongController(
    PhongRepository phongRepo,
    NhaRepository nhaRepo,
    HopDongRepository hopDongRepo,
    PhongDichVuRepository phongDichVuRepo,
    DichVuRepository dichVuRepo,
    PhongService phongService,
    HopDongService hopDongService) : Controller
{
    // GET /Phong
    public async Task<IActionResult> Index()
    {
        await hopDongService.KichHoatHopDongDenHanAsync(DateTime.Today);
        ViewData["ActiveMenu"] = "phong";
        var danhSach = await phongRepo.GetAllTheoTrangThaiHieuLucAsync(DateTime.Today);
        return View(danhSach);
    }

    public async Task<IActionResult> Reconcile()
    {
        ViewData["ActiveMenu"] = "phong";
        return View(await phongService.ReconcileReadOnlyAsync(DateTime.Today));
    }

    public async Task<IActionResult> GanDichVuHangLoat(
        int? dichVuId,
        int? nhaId,
        string trangThai = "DangThue")
    {
        ViewData["ActiveMenu"] = "phong";
        var model = await BuildGanDichVuHangLoatViewModelAsync(dichVuId, nhaId, NormalizeTrangThaiFilter(trangThai));
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GanDichVuHangLoat(
        int dichVuId,
        int? nhaId,
        string trangThai,
        decimal donGia,
        int[] phongIds)
    {
        trangThai = NormalizeTrangThaiFilter(trangThai);

        if (dichVuId <= 0)
            ModelState.AddModelError(nameof(dichVuId), "Vui lòng chọn dịch vụ.");

        if (donGia < 0)
            ModelState.AddModelError(nameof(donGia), "Đơn giá không được âm.");

        if (phongIds.Length == 0)
            ModelState.AddModelError(nameof(phongIds), "Vui lòng chọn ít nhất một phòng.");

        var dichVu = await dichVuRepo.GetByIdAsync(dichVuId);
        if (dichVu == null)
            ModelState.AddModelError(nameof(dichVuId), "Dịch vụ đã chọn không tồn tại.");

        if (!ModelState.IsValid)
        {
            ViewData["ActiveMenu"] = "phong";
            var model = await BuildGanDichVuHangLoatViewModelAsync(dichVuId, nhaId, trangThai);
            model.DonGia = donGia;
            return View(model);
        }

        await phongDichVuRepo.UpsertBulkAsync(phongIds, dichVuId, donGia);
        TempData["Success"] = $"Đã gán mới hoặc bật lại dịch vụ {dichVu!.TenDichVu} cho {phongIds.Distinct().Count()} phòng; đơn giá hiện có được giữ nguyên.";

        return RedirectToAction(nameof(GanDichVuHangLoat), new { dichVuId, nhaId, trangThai });
    }

    // GET /Phong/Details/5
    public async Task<IActionResult> Details(int id)
    {
        await hopDongService.KichHoatHopDongDenHanAsync(DateTime.Today);
        ViewData["ActiveMenu"] = "phong";
        var phong = await phongRepo.GetByIdTheoTrangThaiHieuLucAsync(id, DateTime.Today);
        if (phong == null) return NotFound();

        var hopDong = await hopDongRepo.GetDangHieuLucByPhongAsync(id);
        var dichVuPhong = await phongDichVuRepo.GetByPhongAsync(id);

        ViewBag.HopDong = hopDong;
        ViewBag.DichVuPhong = dichVuPhong;
        return View(phong);
    }

    // GET /Phong/Create
    public async Task<IActionResult> Create()
    {
        ViewData["ActiveMenu"] = "phong";
        await LoadPhongFormDataAsync();
        return View(new Phong());
    }

    // POST /Phong/Create
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Phong phong, int[] dichVuIds, decimal[] donGias)
    {
        await ValidateNhaAsync(phong.NhaId);

        if (!ModelState.IsValid)
        {
            await LoadPhongFormDataAsync();
            return View(phong);
        }

        try
        {
            await phongService.TaoPhongAsync(phong, dichVuIds, donGias);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadPhongFormDataAsync();
            return View(phong);
        }

        TempData["Success"] = "Đã thêm phòng thành công.";
        return RedirectToAction(nameof(Index));
    }

    // GET /Phong/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        ViewData["ActiveMenu"] = "phong";
        var phong = await phongRepo.GetByIdTheoTrangThaiHieuLucAsync(id, DateTime.Today);
        if (phong == null) return NotFound();

        await LoadPhongFormDataAsync(id);
        return View(phong);
    }

    // POST /Phong/Edit/5
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        Phong phong,
        int[] dichVuIds,
        decimal[] donGias,
        bool dangSuaChua)
    {
        if (id != phong.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            await RestoreTrustedFieldsAsync(phong);
            await LoadPhongFormDataAsync(id);
            return View(phong);
        }

        try
        {
            await phongService.SuaPhongAsync(phong, dichVuIds, donGias, dangSuaChua);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await RestoreTrustedFieldsAsync(phong);
            await LoadPhongFormDataAsync(id);
            return View(phong);
        }
        TempData["Success"] = "Đã cập nhật phòng.";
        return RedirectToAction(nameof(Index));
    }

    // POST /Phong/Delete/5
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await phongService.XoaPhongAsync(id);
            TempData["Success"] = "Đã xoá phòng.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadPhongFormDataAsync(int? phongId = null)
    {
        ViewBag.DanhSachNha = await nhaRepo.GetAllAsync();
        ViewBag.DanhSachDichVu = await dichVuRepo.GetAllAsync(hienThi: true);

        if (phongId.HasValue)
        {
            ViewBag.DichVuPhong = await phongDichVuRepo.GetAllByPhongAsync(phongId.Value);
            ViewBag.KhoaDoiNha = await phongService.CoDuLieuNghiepVuAsync(phongId.Value);
        }
    }

    private async Task<GanDichVuHangLoatViewModel> BuildGanDichVuHangLoatViewModelAsync(
        int? dichVuId,
        int? nhaId,
        string trangThai)
    {
        var danhSachDichVu = (await dichVuRepo.GetAllAsync()).ToList();
        var dichVu = dichVuId.HasValue
            ? danhSachDichVu.FirstOrDefault(x => x.Id == dichVuId.Value)
            : danhSachDichVu.FirstOrDefault(x => x.LoaiTinhPhi == DichVu.LoaiCoDinh && x.CachTinhCoDinh == DichVu.CachTinhTheoNguoi)
              ?? danhSachDichVu.FirstOrDefault();

        var selectedDichVuId = dichVu?.Id;
        var rows = selectedDichVuId.HasValue
            ? (await phongDichVuRepo.GetBulkAssignmentRowsAsync(selectedDichVuId.Value, nhaId, trangThai)).ToList()
            : [];

        return new GanDichVuHangLoatViewModel
        {
            NhaId = nhaId,
            DichVuId = selectedDichVuId,
            TrangThai = trangThai,
            DonGia = dichVu?.DonGiaMacDinh ?? 0,
            DanhSachNha = (await nhaRepo.GetAllAsync()).ToList(),
            DanhSachDichVu = danhSachDichVu,
            Rows = rows
        };
    }

    private static string NormalizeTrangThaiFilter(string? trangThai)
        => trangThai is "TatCa" or "DangThue" or "Trong" or "DangSuaChua"
            ? trangThai
            : "DangThue";

    private async Task ValidateNhaAsync(int nhaId)
    {
        if (nhaId <= 0)
        {
            ModelState.AddModelError(nameof(Phong.NhaId), "Vui lòng chọn Nhà.");
            return;
        }

        var nha = await nhaRepo.GetByIdAsync(nhaId);
        if (nha == null)
        {
            ModelState.AddModelError(nameof(Phong.NhaId), "Nhà đã chọn không tồn tại.");
        }
    }

    private async Task RestoreTrustedFieldsAsync(Phong phong)
    {
        var banGoc = await phongRepo.GetByIdAsync(phong.Id);
        if (banGoc == null) return;

        phong.NhaId = banGoc.NhaId;
        phong.TrangThai = banGoc.TrangThai;
        ModelState.Remove(nameof(Phong.NhaId));
        ModelState.Remove(nameof(Phong.TrangThai));
    }
}
