using Microsoft.AspNetCore.Mvc;
using QuanLyNhaTro.Models;
using QuanLyNhaTro.Repositories;

namespace QuanLyNhaTro.Controllers;

public class ChiSoNgoaiHopDongController(
    ChiSoNgoaiHopDongRepository chiSoNgoaiHopDongRepo,
    ChiSoDienNuocRepository chiSoRepo,
    PhongRepository phongRepo,
    DichVuRepository dichVuRepo,
    HopDongRepository hopDongRepo) : Controller
{
    public async Task<IActionResult> Index(int? phongId, int? dichVuId)
    {
        ViewData["ActiveMenu"] = "chiso-ngoai";
        await LoadFormDataAsync(phongId, dichVuId);
        return View(await chiSoNgoaiHopDongRepo.GetAllAsync(phongId, dichVuId));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ChiSoNgoaiHopDong model)
    {
        var errors = await ValidateAsync(model);
        if (errors.Count > 0)
        {
            TempData["Error"] = "Khong the luu chi so ngoai hop dong. " + string.Join(" ", errors);
            return RedirectToAction(nameof(Index), new { phongId = model.PhongId, dichVuId = model.DichVuId });
        }

        model.LyDo = model.LyDo.Trim();
        model.GhiChu = string.IsNullOrWhiteSpace(model.GhiChu) ? null : model.GhiChu.Trim();
        await chiSoNgoaiHopDongRepo.InsertAsync(model);

        TempData["Success"] = "Da ghi nhan chi so ngoai hop dong.";
        return RedirectToAction(nameof(Index), new { phongId = model.PhongId, dichVuId = model.DichVuId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await chiSoNgoaiHopDongRepo.GetByIdAsync(id);
        if (existing == null) return NotFound();

        await chiSoNgoaiHopDongRepo.DeleteAsync(id);
        TempData["Success"] = "Da xoa chi so ngoai hop dong.";
        return RedirectToAction(nameof(Index), new { phongId = existing.PhongId, dichVuId = existing.DichVuId });
    }

    private async Task LoadFormDataAsync(int? phongId, int? dichVuId)
    {
        ViewBag.PhongId = phongId;
        ViewBag.DichVuId = dichVuId;
        ViewBag.DanhSachPhong = (await phongRepo.GetAllAsync()).ToList();
        ViewBag.DichVuTheoChiSo = (await dichVuRepo.GetAllAsync())
            .Where(dv => dv.LoaiTinhPhi == DichVu.LoaiTheoChiSo)
            .ToList();

        if (phongId.HasValue && dichVuId.HasValue)
        {
            var chiSoGanNhat = await chiSoRepo.GetLatestAsync(phongId.Value, dichVuId.Value);
            var ngoaiHopDongGanNhat = await chiSoNgoaiHopDongRepo.GetLatestAsync(phongId.Value, dichVuId.Value);
            ViewBag.GoiYTuChiSo = ResolveGoiYTuChiSo(chiSoGanNhat, ngoaiHopDongGanNhat);
        }
    }

    private static decimal? ResolveGoiYTuChiSo(
        ChiSoDienNuoc? chiSoGanNhat,
        ChiSoNgoaiHopDong? ngoaiHopDongGanNhat)
    {
        if (chiSoGanNhat == null)
            return ngoaiHopDongGanNhat?.DenChiSo;

        if (ngoaiHopDongGanNhat == null)
            return chiSoGanNhat.ChiSoCuoi;

        var ngayChiSo = chiSoGanNhat.NgayDoc?.Date ?? new DateTime(
            chiSoGanNhat.Nam,
            chiSoGanNhat.Thang,
            DateTime.DaysInMonth(chiSoGanNhat.Nam, chiSoGanNhat.Thang));

        return ngoaiHopDongGanNhat.NgayGhiNhan.Date >= ngayChiSo
            ? ngoaiHopDongGanNhat.DenChiSo
            : chiSoGanNhat.ChiSoCuoi;
    }

    private async Task<List<string>> ValidateAsync(ChiSoNgoaiHopDong model)
    {
        var errors = new List<string>();
        var phong = model.PhongId > 0 ? await phongRepo.GetByIdAsync(model.PhongId) : null;
        if (phong == null)
            errors.Add("Phong khong hop le.");

        var dichVu = model.DichVuId > 0 ? await dichVuRepo.GetByIdAsync(model.DichVuId) : null;
        if (dichVu == null || dichVu.LoaiTinhPhi != DichVu.LoaiTheoChiSo)
            errors.Add("Dich vu phai la loai TheoChiSo.");

        if (model.TuChiSo < 0 || model.DenChiSo < 0)
            errors.Add("Chi so khong duoc am.");

        if (model.DenChiSo < model.TuChiSo)
            errors.Add("Chi so den phai lon hon hoac bang chi so tu.");

        if (model.NgayGhiNhan == default)
            errors.Add("Ngay ghi nhan la bat buoc.");
        else if (phong != null)
        {
            var hopDongTrongNgay = await hopDongRepo.GetByPhongAndDateAsync(model.PhongId, model.NgayGhiNhan);
            if (hopDongTrongNgay != null)
                errors.Add("Ngay ghi nhan dang thuoc mot hop dong cua phong. Hay nhap chi so theo hop dong, hoac ghi phat sinh ngoai hop dong tu ngay sau khi khach tra phong.");
        }

        if (string.IsNullOrWhiteSpace(model.LyDo))
            errors.Add("Ly do la bat buoc.");

        return errors;
    }
}
