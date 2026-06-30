using Microsoft.AspNetCore.Mvc;
using QuanLyNhaTro.Models;
using QuanLyNhaTro.Repositories;
using QuanLyNhaTro.Services;

namespace QuanLyNhaTro.Controllers;

public class ChiSoController(
    ChiSoDienNuocRepository chiSoRepo,
    HopDongRepository hopDongRepo,
    DichVuRepository dichVuRepo,
    PhongRepository phongRepo,
    PhongDichVuRepository phongDichVuRepo) : Controller
{
    public async Task<IActionResult> Index(int? thang, int? nam)
    {
        ViewData["ActiveMenu"] = "chiso";
        thang ??= DateTime.Today.Month;
        nam ??= DateTime.Today.Year;

        var hopDongs = (await hopDongRepo.GetAllAsync())
            .Where(hd => hd.TrangThai == "DangHieuLuc")
            .ToList();

        var dsChiSo = new List<ChiSoDienNuoc>();
        foreach (var hd in hopDongs)
        {
            var chiSoKy = await chiSoRepo.GetByHopDongKyAsync(hd.Id, thang.Value, nam.Value);
            dsChiSo.AddRange(chiSoKy);
        }

        ViewBag.Thang = thang;
        ViewBag.Nam = nam;
        ViewBag.HopDongs = hopDongs;
        ViewBag.DsChiSo = dsChiSo;
        return View();
    }

    public async Task<IActionResult> NhapHangLoat(int? thang, int? nam)
    {
        ViewData["ActiveMenu"] = "chiso";
        thang ??= DateTime.Today.Month;
        nam ??= DateTime.Today.Year;

        var model = await BuildNhapHangLoatViewModelAsync(thang.Value, nam.Value);
        return View(model);
    }

    public async Task<IActionResult> Nhap(int hopDongId, int? thang, int? nam)
    {
        ViewData["ActiveMenu"] = "chiso";
        thang ??= DateTime.Today.Month;
        nam ??= DateTime.Today.Year;

        var hopDong = await hopDongRepo.GetByIdAsync(hopDongId);
        if (hopDong == null) return NotFound();

        var formData = await LoadChiSoFormAsync(hopDong.PhongId, thang.Value, nam.Value);

        ViewBag.HopDong = hopDong;
        ViewBag.Thang = thang;
        ViewBag.Nam = nam;
        ViewBag.DichVuTheoChiSo = formData.DichVuTheoChiSo;
        ViewBag.ChiSoHienTai = formData.ChiSoHienTai.ToDictionary(cs => cs.DichVuId);
        ViewBag.ChiSoDauTheoDichVu = formData.ChiSoDauTheoDichVu;
        return View();
    }

    public async Task<IActionResult> NhapTheoPhong(int phongId, int? thang, int? nam, int? returnHopDongId)
    {
        ViewData["ActiveMenu"] = "chiso";
        thang ??= DateTime.Today.Month;
        nam ??= DateTime.Today.Year;

        var phong = await phongRepo.GetByIdAsync(phongId);
        if (phong == null) return NotFound();

        var formData = await LoadChiSoFormAsync(phongId, thang.Value, nam.Value);

        ViewBag.Phong = phong;
        ViewBag.Thang = thang;
        ViewBag.Nam = nam;
        ViewBag.ReturnHopDongId = returnHopDongId;
        ViewBag.DichVuTheoChiSo = formData.DichVuTheoChiSo;
        ViewBag.ChiSoHienTai = formData.ChiSoHienTai.ToDictionary(cs => cs.DichVuId);
        ViewBag.ChiSoDauTheoDichVu = formData.ChiSoDauTheoDichVu;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Nhap(
        int hopDongId,
        int thang,
        int nam,
        int[] dichVuIds,
        decimal[] chiSoCuois,
        int[] chiSoIds,
        string[] loaiGhiNhans,
        decimal?[] chiSoTruocResets,
        decimal?[] chiSoSauResets,
        string?[] lyDoDieuChinhs)
    {
        var hopDong = await hopDongRepo.GetByIdAsync(hopDongId)
            ?? throw new InvalidOperationException($"Khong tim thay hop dong #{hopDongId}.");

        var errors = await SaveChiSoAsync(
            hopDong.PhongId,
            thang,
            nam,
            dichVuIds,
            chiSoCuois,
            chiSoIds,
            loaiGhiNhans,
            chiSoTruocResets,
            chiSoSauResets,
            lyDoDieuChinhs);

        if (errors.Count > 0)
        {
            TempData["Error"] = "Khong the luu chi so. " + string.Join(" ", errors);
            return RedirectToAction(nameof(Nhap), new { hopDongId, thang, nam });
        }

        TempData["Success"] = $"Da luu chi so ky {thang}/{nam}.";
        return RedirectToAction(nameof(Index), new { thang, nam });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> NhapTheoPhong(
        int phongId,
        int thang,
        int nam,
        int? returnHopDongId,
        int[] dichVuIds,
        decimal[] chiSoCuois,
        int[] chiSoIds,
        string[] loaiGhiNhans,
        decimal?[] chiSoTruocResets,
        decimal?[] chiSoSauResets,
        string?[] lyDoDieuChinhs)
    {
        var phong = await phongRepo.GetByIdAsync(phongId);
        if (phong == null) return NotFound();

        var errors = await SaveChiSoAsync(
            phongId,
            thang,
            nam,
            dichVuIds,
            chiSoCuois,
            chiSoIds,
            loaiGhiNhans,
            chiSoTruocResets,
            chiSoSauResets,
            lyDoDieuChinhs);

        if (errors.Count > 0)
        {
            TempData["Error"] = "Khong the luu chi so. " + string.Join(" ", errors);
            return RedirectToAction(nameof(NhapTheoPhong), new { phongId, thang, nam, returnHopDongId });
        }

        TempData["Success"] = $"Da luu chi so phong {phong.TenPhong} ky {thang}/{nam}.";
        if (returnHopDongId.HasValue && returnHopDongId.Value > 0)
        {
            return RedirectToAction("Create", "ChuyenPhong", new { hopDongId = returnHopDongId.Value });
        }

        return RedirectToAction(nameof(Index), new { thang, nam });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> NhapHangLoat(ChiSoHangLoatViewModel model)
    {
        ViewData["ActiveMenu"] = "chiso";

        if (model.Thang < 1 || model.Thang > 12 || model.Nam < 2000 || model.Nam > 2099)
        {
            TempData["Error"] = "Ky nhap chi so khong hop le.";
            return RedirectToAction(nameof(NhapHangLoat), new { thang = DateTime.Today.Month, nam = DateTime.Today.Year });
        }

        var selectedRows = model.Rows.Where(r => r.Luu).ToList();
        if (selectedRows.Count == 0)
        {
            TempData["Error"] = "Chua chon dong nao de luu.";
            return View(model);
        }

        var allItems = new List<ChiSoNhapItem>();
        var allErrors = new List<string>();

        foreach (var group in selectedRows.GroupBy(r => r.PhongId))
        {
            var rows = group.ToList();
            var result = await ValidateChiSoAsync(
                group.Key,
                model.Thang,
                model.Nam,
                rows.Select(r => r.DichVuId).ToArray(),
                rows.Select(r => r.ChiSoCuoi).ToArray(),
                rows.Select(r => r.ChiSoId).ToArray(),
                rows.Select(r => r.LoaiGhiNhan).ToArray(),
                rows.Select(r => r.ChiSoTruocReset).ToArray(),
                rows.Select(r => r.ChiSoSauReset).ToArray(),
                rows.Select(r => r.LyDoDieuChinh).ToArray());

            allItems.AddRange(result.Items);
            allErrors.AddRange(result.Errors.Select(error =>
            {
                var tenPhong = rows.FirstOrDefault()?.TenPhong;
                return string.IsNullOrWhiteSpace(tenPhong) ? error : $"{tenPhong}: {error}";
            }));
        }

        if (allErrors.Count > 0)
        {
            TempData["Error"] = "Khong the luu chi so. " + string.Join(" ", allErrors);
            return View(model);
        }

        await SaveChiSoItemsAsync(allItems);
        TempData["Success"] = $"Da luu {allItems.Count} dong chi so ky {model.Thang}/{model.Nam}.";
        return RedirectToAction(nameof(Index), new { thang = model.Thang, nam = model.Nam });
    }

    private async Task<ChiSoFormData> LoadChiSoFormAsync(int phongId, int thang, int nam)
    {
        var dichVuTheoChiSo = (await phongDichVuRepo.GetByPhongAsync(phongId))
            .Where(pdv => pdv.DichVu?.LoaiTinhPhi == "TheoChiSo")
            .Select(pdv => pdv.DichVu!)
            .GroupBy(dv => dv.Id)
            .Select(g => g.First())
            .ToList();

        var chiSoHienTai = (await chiSoRepo.GetByPhongKyAsync(phongId, thang, nam)).ToList();
        var chiSoDauTheoDichVu = new Dictionary<int, decimal>();

        foreach (var dv in dichVuTheoChiSo)
        {
            var current = chiSoHienTai.FirstOrDefault(cs => cs.DichVuId == dv.Id);
            if (current != null)
            {
                chiSoDauTheoDichVu[dv.Id] = current.ChiSoDau;
                continue;
            }

            var kyTruoc = await chiSoRepo.GetChiSoCuoiKyTruocAsync(phongId, dv.Id, thang, nam);
            chiSoDauTheoDichVu[dv.Id] = kyTruoc?.ChiSoCuoi ?? 0;
        }

        return new ChiSoFormData(dichVuTheoChiSo, chiSoHienTai, chiSoDauTheoDichVu);
    }

    private async Task<ChiSoHangLoatViewModel> BuildNhapHangLoatViewModelAsync(int thang, int nam)
    {
        var hopDongs = (await hopDongRepo.GetAllAsync())
            .Where(hd => hd.TrangThai == "DangHieuLuc")
            .OrderBy(hd => hd.Phong?.TenPhong)
            .ToList();

        var rows = new List<ChiSoHangLoatRowViewModel>();
        foreach (var hopDong in hopDongs)
        {
            var formData = await LoadChiSoFormAsync(hopDong.PhongId, thang, nam);
            foreach (var dichVu in formData.DichVuTheoChiSo)
            {
                var current = formData.ChiSoHienTai.FirstOrDefault(cs => cs.DichVuId == dichVu.Id);
                var chiSoDau = formData.ChiSoDauTheoDichVu.TryGetValue(dichVu.Id, out var dau) ? dau : 0;
                var loaiGhiNhan = current?.LoaiGhiNhan ?? ChiSoDienNuoc.LoaiBinhThuong;

                rows.Add(new ChiSoHangLoatRowViewModel
                {
                    Luu = true,
                    PhongId = hopDong.PhongId,
                    HopDongId = hopDong.Id,
                    TenPhong = hopDong.Phong?.TenPhong ?? $"Phong #{hopDong.PhongId}",
                    DichVuId = dichVu.Id,
                    TenDichVu = dichVu.TenDichVu,
                    DonViTinh = dichVu.DonViTinh,
                    ChiSoId = current?.Id ?? 0,
                    ChiSoDau = chiSoDau,
                    ChiSoCuoi = current?.ChiSoCuoi ?? chiSoDau,
                    LoaiGhiNhan = loaiGhiNhan,
                    ChiSoTruocReset = current?.ChiSoTruocReset,
                    ChiSoSauReset = current?.ChiSoSauReset,
                    LyDoDieuChinh = current?.LyDoDieuChinh,
                    DaNhap = current != null,
                    SanLuongHienTai = current?.SoLuongTieuThu
                });
            }
        }

        return new ChiSoHangLoatViewModel
        {
            Thang = thang,
            Nam = nam,
            Rows = rows
        };
    }

    private async Task<List<string>> SaveChiSoAsync(
        int phongId,
        int thang,
        int nam,
        int[] dichVuIds,
        decimal[] chiSoCuois,
        int[] chiSoIds,
        string[] loaiGhiNhans,
        decimal?[] chiSoTruocResets,
        decimal?[] chiSoSauResets,
        string?[] lyDoDieuChinhs)
    {
        var result = await ValidateChiSoAsync(
            phongId,
            thang,
            nam,
            dichVuIds,
            chiSoCuois,
            chiSoIds,
            loaiGhiNhans,
            chiSoTruocResets,
            chiSoSauResets,
            lyDoDieuChinhs);

        if (result.Errors.Count > 0)
        {
            return result.Errors;
        }

        await SaveChiSoItemsAsync(result.Items);
        return result.Errors;
    }

    private async Task<ChiSoValidationResult> ValidateChiSoAsync(
        int phongId,
        int thang,
        int nam,
        int[] dichVuIds,
        decimal[] chiSoCuois,
        int[] chiSoIds,
        string[] loaiGhiNhans,
        decimal?[] chiSoTruocResets,
        decimal?[] chiSoSauResets,
        string?[] lyDoDieuChinhs)
    {
        var items = new List<ChiSoNhapItem>();
        var errors = new List<string>();
        var chiSoHienTai = (await chiSoRepo.GetByPhongKyAsync(phongId, thang, nam))
            .ToDictionary(cs => cs.Id);
        var dichVuTheoChiSoIds = (await phongDichVuRepo.GetByPhongAsync(phongId))
            .Where(pdv => pdv.DichVu?.LoaiTinhPhi == "TheoChiSo")
            .Select(pdv => pdv.DichVuId)
            .ToHashSet();

        for (int i = 0; i < dichVuIds.Length; i++)
        {
            var dichVuId = dichVuIds[i];
            if (!dichVuTheoChiSoIds.Contains(dichVuId))
            {
                var dichVu = await dichVuRepo.GetByIdAsync(dichVuId);
                errors.Add($"{dichVu?.TenDichVu ?? $"Dich vu #{dichVuId}"}: dich vu chua duoc gan cho phong hoac khong phai loai theo chi so.");
                continue;
            }

            var chiSoCuoi = i < chiSoCuois.Length ? chiSoCuois[i] : 0;
            var existId = i < chiSoIds.Length ? chiSoIds[i] : 0;
            var loaiGhiNhan = i < loaiGhiNhans.Length && !string.IsNullOrWhiteSpace(loaiGhiNhans[i])
                ? loaiGhiNhans[i]
                : ChiSoDienNuoc.LoaiBinhThuong;
            var chiSoTruocReset = i < chiSoTruocResets.Length ? chiSoTruocResets[i] : null;
            var chiSoSauReset = i < chiSoSauResets.Length ? chiSoSauResets[i] : null;
            var lyDoDieuChinh = i < lyDoDieuChinhs.Length ? lyDoDieuChinhs[i] : null;
            ChiSoDienNuoc? current = null;
            if (existId > 0 && !chiSoHienTai.TryGetValue(existId, out current))
            {
                errors.Add($"Chi so #{existId}: khong thuoc phong/ky dang nhap.");
                continue;
            }

            var kyTruoc = current == null
                ? await chiSoRepo.GetChiSoCuoiKyTruocAsync(phongId, dichVuId, thang, nam)
                : null;
            var chiSoDau = current?.ChiSoDau ?? kyTruoc?.ChiSoCuoi ?? 0;
            var chiSo = new ChiSoDienNuoc
            {
                Id = existId,
                PhongId = phongId,
                DichVuId = dichVuId,
                Thang = thang,
                Nam = nam,
                ChiSoDau = chiSoDau,
                ChiSoCuoi = chiSoCuoi,
                LoaiGhiNhan = loaiGhiNhan,
                ChiSoTruocReset = loaiGhiNhan == ChiSoDienNuoc.LoaiReset ? chiSoTruocReset : null,
                ChiSoSauReset = loaiGhiNhan == ChiSoDienNuoc.LoaiReset ? chiSoSauReset : null,
                LyDoDieuChinh = loaiGhiNhan == ChiSoDienNuoc.LoaiReset ? lyDoDieuChinh : null,
                NgayDoc = DateTime.Today
            };

            items.Add(new ChiSoNhapItem(chiSo));

            if (loaiGhiNhan != ChiSoDienNuoc.LoaiBinhThuong && loaiGhiNhan != ChiSoDienNuoc.LoaiReset)
            {
                var dichVu = await dichVuRepo.GetByIdAsync(dichVuId);
                errors.Add($"{dichVu?.TenDichVu ?? $"Dich vu #{dichVuId}"}: loai ghi nhan khong hop le.");
                continue;
            }

            if (loaiGhiNhan == ChiSoDienNuoc.LoaiReset && string.IsNullOrWhiteSpace(lyDoDieuChinh))
            {
                var dichVu = await dichVuRepo.GetByIdAsync(dichVuId);
                errors.Add($"{dichVu?.TenDichVu ?? $"Dich vu #{dichVuId}"}: can nhap ly do khi chon Reset.");
                continue;
            }

            try
            {
                _ = ChiSoConsumptionCalculator.Calculate(chiSo);
            }
            catch (InvalidOperationException ex)
            {
                errors.Add(ex.Message);
            }
        }

        return new ChiSoValidationResult(items, errors);
    }

    private async Task SaveChiSoItemsAsync(IEnumerable<ChiSoNhapItem> items)
    {
        foreach (var item in items)
        {
            if (item.ChiSo.Id > 0)
            {
                await chiSoRepo.UpdateAsync(item.ChiSo);
            }
            else
            {
                await chiSoRepo.InsertAsync(item.ChiSo);
            }
        }
    }

    private sealed record ChiSoNhapItem(ChiSoDienNuoc ChiSo);
    private sealed record ChiSoValidationResult(List<ChiSoNhapItem> Items, List<string> Errors);
    private sealed record ChiSoFormData(
        List<DichVu> DichVuTheoChiSo,
        List<ChiSoDienNuoc> ChiSoHienTai,
        Dictionary<int, decimal> ChiSoDauTheoDichVu);
}
