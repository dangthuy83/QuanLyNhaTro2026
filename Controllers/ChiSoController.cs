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
    PhongDichVuRepository phongDichVuRepo,
    HopDongDichVuRepository hopDongDichVuRepo,
    LichSuHinhThucDichVuRepository lichSuHinhThucRepo,
    ChiSoService chiSoService,
    MeterContinuityService continuity) : Controller
{
    public async Task<IActionResult> Index(int? thang, int? nam)
    {
        ViewData["ActiveMenu"] = "chiso";
        var ky = DefaultBillingPeriodResolver.Resolve(thang, nam);
        thang = ky.Thang;
        nam = ky.Nam;

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
        var ky = DefaultBillingPeriodResolver.Resolve(thang, nam);
        thang = ky.Thang;
        nam = ky.Nam;

        var model = await BuildNhapHangLoatViewModelAsync(thang.Value, nam.Value);
        return View(model);
    }

    public async Task<IActionResult> Nhap(int hopDongId, int? thang, int? nam)
    {
        ViewData["ActiveMenu"] = "chiso";
        var ky = DefaultBillingPeriodResolver.Resolve(thang, nam);
        thang = ky.Thang;
        nam = ky.Nam;

        var hopDong = await hopDongRepo.GetByIdAsync(hopDongId);
        if (hopDong == null) return NotFound();

        var formData = await LoadChiSoFormAsync(hopDong.PhongId, thang.Value, nam.Value, hopDong.Id, hopDong.NgayBatDau);

        ViewBag.HopDong = hopDong;
        ViewBag.Thang = thang;
        ViewBag.Nam = nam;
        ViewBag.DichVuTheoChiSo = formData.DichVuTheoChiSo;
        ViewBag.ChiSoHienTai = formData.ChiSoHienTai.ToDictionary(cs => cs.DichVuId);
        ViewBag.ChiSoDauTheoDichVu = formData.ChiSoDauTheoDichVu;
        ViewBag.ChoNhapChiSoDauTheoDichVu = formData.ChoNhapChiSoDauTheoDichVu;
        ViewBag.NguonChiSoDauTheoDichVu = formData.NguonChiSoDauTheoDichVu;
        return View();
    }

    public async Task<IActionResult> NhapTheoPhong(int phongId, int? thang, int? nam, int? returnHopDongId)
    {
        ViewData["ActiveMenu"] = "chiso";
        var ky = DefaultBillingPeriodResolver.Resolve(thang, nam);
        thang = ky.Thang;
        nam = ky.Nam;

        var phong = await phongRepo.GetByIdAsync(phongId);
        if (phong == null) return NotFound();

        var formData = await LoadChiSoFormAsync(phongId, thang.Value, nam.Value, null, null);

        ViewBag.Phong = phong;
        ViewBag.Thang = thang;
        ViewBag.Nam = nam;
        ViewBag.ReturnHopDongId = returnHopDongId;
        ViewBag.DichVuTheoChiSo = formData.DichVuTheoChiSo;
        ViewBag.ChiSoHienTai = formData.ChiSoHienTai.ToDictionary(cs => cs.DichVuId);
        ViewBag.ChiSoDauTheoDichVu = formData.ChiSoDauTheoDichVu;
        ViewBag.ChoNhapChiSoDauTheoDichVu = formData.ChoNhapChiSoDauTheoDichVu;
        ViewBag.NguonChiSoDauTheoDichVu = formData.NguonChiSoDauTheoDichVu;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Nhap(
        int hopDongId,
        int thang,
        int nam,
        int[] dichVuIds,
        decimal[] chiSoDaus,
        decimal[] chiSoCuois,
        int[] chiSoIds,
        DateTime?[] ngayDocs,
        string[] loaiGhiNhans,
        decimal?[] chiSoTruocResets,
        decimal?[] chiSoSauResets,
        string?[] lyDoDieuChinhs)
    {
        var hopDong = await hopDongRepo.GetByIdAsync(hopDongId)
            ?? throw new InvalidOperationException($"Khong tim thay hop dong #{hopDongId}.");

        var errors = await SaveChiSoAsync(
            hopDong.PhongId,
            hopDong.Id,
            hopDong.NgayBatDau,
            thang,
            nam,
            dichVuIds,
            chiSoDaus,
            chiSoCuois,
            chiSoIds,
            ngayDocs,
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
        decimal[] chiSoDaus,
        decimal[] chiSoCuois,
        int[] chiSoIds,
        DateTime?[] ngayDocs,
        string[] loaiGhiNhans,
        decimal?[] chiSoTruocResets,
        decimal?[] chiSoSauResets,
        string?[] lyDoDieuChinhs)
    {
        var phong = await phongRepo.GetByIdAsync(phongId);
        if (phong == null) return NotFound();

        var errors = await SaveChiSoAsync(
            phongId,
            null,
            null,
            thang,
            nam,
            dichVuIds,
            chiSoDaus,
            chiSoCuois,
            chiSoIds,
            ngayDocs,
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

        if (!BusinessDataLimits.IsValidPeriod(model.Thang, model.Nam))
        {
            TempData["Error"] = "Ky nhap chi so khong hop le.";
            var kyMacDinh = DefaultBillingPeriodResolver.Resolve();
            return RedirectToAction(nameof(NhapHangLoat), new { thang = kyMacDinh.Thang, nam = kyMacDinh.Nam });
        }

        var selectedRows = model.Rows.Where(r => r.Luu).ToList();
        if (selectedRows.Count == 0)
        {
            TempData["Error"] = "Chua chon dong nao de luu.";
            return View(model);
        }

        var allItems = new List<ChiSoNhapItem>();
        var allErrors = new List<string>();

        foreach (var group in selectedRows.GroupBy(r => new { r.PhongId, r.HopDongId, r.NgayBatDauHopDong }))
        {
            var rows = group.ToList();
            var result = await ValidateChiSoAsync(
                group.Key.PhongId,
                group.Key.HopDongId,
                group.Key.NgayBatDauHopDong,
                model.Thang,
                model.Nam,
                rows.Select(r => r.DichVuId).ToArray(),
                rows.Select(r => r.ChiSoDau).ToArray(),
                rows.Select(r => r.ChiSoCuoi).ToArray(),
                rows.Select(r => r.ChiSoId).ToArray(),
                rows.Select(r => r.NgayDoc).ToArray(),
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

        try
        {
            await SaveChiSoItemsAsync(allItems);
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = "Khong the luu chi so. " + ex.Message;
            return View(model);
        }
        TempData["Success"] = $"Da luu {allItems.Count} dong chi so ky {model.Thang}/{model.Nam}.";
        return RedirectToAction(nameof(Index), new { thang = model.Thang, nam = model.Nam });
    }

    private async Task<ChiSoFormData> LoadChiSoFormAsync(
        int phongId,
        int thang,
        int nam,
        int? hopDongId,
        DateTime? ngayBatDauHopDong)
    {
        var dichVuNguon = hopDongId.HasValue
            ? await hopDongDichVuRepo.GetPhongDichVuByHopDongKyAsync(hopDongId.Value, thang, nam)
            : await phongDichVuRepo.GetByPhongKyAsync(phongId, thang, nam);
        var dichVuTheoChiSo = dichVuNguon
            .Where(pdv => pdv.DichVu?.LoaiTinhPhi == "TheoChiSo")
            .Select(pdv => pdv.DichVu!)
            .GroupBy(dv => dv.Id)
            .Select(g => g.First())
            .ToList();

        var chiSoHienTai = (await chiSoRepo.GetByPhongKyAsync(phongId, thang, nam, hopDongId)).ToList();
        var chiSoDauTheoDichVu = new Dictionary<int, decimal>();
        var choNhapChiSoDauTheoDichVu = new Dictionary<int, bool>();
        var nguonChiSoDauTheoDichVu = new Dictionary<int, string>();

        foreach (var dv in dichVuTheoChiSo)
        {
            var current = chiSoHienTai.FirstOrDefault(cs => cs.DichVuId == dv.Id);
            if (current != null)
            {
                chiSoDauTheoDichVu[dv.Id] = current.ChiSoDau;
                choNhapChiSoDauTheoDichVu[dv.Id] = false;
                nguonChiSoDauTheoDichVu[dv.Id] = "Đã nhập trong kỳ này";
                continue;
            }

            var startInfo = await ResolveChiSoDauAsync(phongId, hopDongId, ngayBatDauHopDong, dv.Id, thang, nam, null, null);
            chiSoDauTheoDichVu[dv.Id] = startInfo.ChiSoDau;
            choNhapChiSoDauTheoDichVu[dv.Id] = startInfo.ChoNhapTay;
            nguonChiSoDauTheoDichVu[dv.Id] = startInfo.Nguon;
        }

        return new ChiSoFormData(dichVuTheoChiSo, chiSoHienTai, chiSoDauTheoDichVu, choNhapChiSoDauTheoDichVu, nguonChiSoDauTheoDichVu);
    }

    private async Task<ChiSoHangLoatViewModel> BuildNhapHangLoatViewModelAsync(int thang, int nam)
    {
        var kyBatDau = new DateTime(nam, thang, 1);
        var kyKetThuc = new DateTime(nam, thang, DateTime.DaysInMonth(nam, thang));
        var hopDongs = (await hopDongRepo.GetAllAsync())
            .Where(hd => hd.TrangThai == "DangHieuLuc")
            .Where(hd => hd.NgayBatDau.Date <= kyKetThuc &&
                         (!hd.NgayKetThuc.HasValue || hd.NgayKetThuc.Value.Date >= kyBatDau))
            .OrderBy(hd => hd.Phong?.TenPhong)
            .ToList();

        var rows = new List<ChiSoHangLoatRowViewModel>();
        foreach (var hopDong in hopDongs)
        {
            var formData = await LoadChiSoFormAsync(hopDong.PhongId, thang, nam, hopDong.Id, hopDong.NgayBatDau);
            foreach (var dichVu in formData.DichVuTheoChiSo)
            {
                var current = formData.ChiSoHienTai.FirstOrDefault(cs => cs.DichVuId == dichVu.Id);
                var chiSoDau = formData.ChiSoDauTheoDichVu.TryGetValue(dichVu.Id, out var dau) ? dau : 0;
                var choNhapChiSoDau = formData.ChoNhapChiSoDauTheoDichVu.TryGetValue(dichVu.Id, out var choNhapDau) && choNhapDau;
                var nguonChiSoDau = formData.NguonChiSoDauTheoDichVu.TryGetValue(dichVu.Id, out var nguon) ? nguon : "";
                var loaiGhiNhan = current?.LoaiGhiNhan ?? ChiSoDienNuoc.LoaiBinhThuong;

                rows.Add(new ChiSoHangLoatRowViewModel
                {
                    Luu = true,
                    PhongId = hopDong.PhongId,
                    HopDongId = hopDong.Id,
                    NgayBatDauHopDong = hopDong.NgayBatDau,
                    TenPhong = hopDong.Phong?.TenPhong ?? $"Phong #{hopDong.PhongId}",
                    DichVuId = dichVu.Id,
                    TenDichVu = dichVu.TenDichVu,
                    DonViTinh = dichVu.DonViTinh,
                    ChiSoId = current?.Id ?? 0,
                    ChiSoDau = chiSoDau,
                    NguonChiSoDau = nguonChiSoDau,
                    ChiSoCuoi = current?.ChiSoCuoi ?? chiSoDau,
                    NgayDoc = current?.NgayDoc ?? ResolveNgayDocMacDinh(thang, nam, hopDong),
                    ChoNhapChiSoDau = choNhapChiSoDau,
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
        int? hopDongId,
        DateTime? ngayBatDauHopDong,
        int thang,
        int nam,
        int[] dichVuIds,
        decimal[] chiSoDaus,
        decimal[] chiSoCuois,
        int[] chiSoIds,
        DateTime?[] ngayDocs,
        string[] loaiGhiNhans,
        decimal?[] chiSoTruocResets,
        decimal?[] chiSoSauResets,
        string?[] lyDoDieuChinhs)
    {
        var result = await ValidateChiSoAsync(
            phongId,
            hopDongId,
            ngayBatDauHopDong,
            thang,
            nam,
            dichVuIds,
            chiSoDaus,
            chiSoCuois,
            chiSoIds,
            ngayDocs,
            loaiGhiNhans,
            chiSoTruocResets,
            chiSoSauResets,
            lyDoDieuChinhs);

        if (result.Errors.Count > 0)
        {
            return result.Errors;
        }

        try
        {
            await SaveChiSoItemsAsync(result.Items);
        }
        catch (InvalidOperationException ex)
        {
            result.Errors.Add(ex.Message);
        }
        return result.Errors;
    }

    private async Task<ChiSoValidationResult> ValidateChiSoAsync(
        int phongId,
        int? hopDongId,
        DateTime? ngayBatDauHopDong,
        int thang,
        int nam,
        int[] dichVuIds,
        decimal[] chiSoDaus,
        decimal[] chiSoCuois,
        int[] chiSoIds,
        DateTime?[] ngayDocs,
        string[] loaiGhiNhans,
        decimal?[] chiSoTruocResets,
        decimal?[] chiSoSauResets,
        string?[] lyDoDieuChinhs)
    {
        var items = new List<ChiSoNhapItem>();
        var errors = new List<string>();
        var chiSoHienTai = (await chiSoRepo.GetByPhongKyAsync(phongId, thang, nam, hopDongId))
            .ToDictionary(cs => cs.Id);
        var dichVuNguon = hopDongId.HasValue
            ? await hopDongDichVuRepo.GetPhongDichVuByHopDongKyAsync(hopDongId.Value, thang, nam)
            : await phongDichVuRepo.GetByPhongKyAsync(phongId, thang, nam);
        var dichVuTheoChiSoIds = dichVuNguon
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

            var chiSoDauNhap = i < chiSoDaus.Length ? chiSoDaus[i] : 0;
            var chiSoCuoi = i < chiSoCuois.Length ? chiSoCuois[i] : 0;
            var existId = i < chiSoIds.Length ? chiSoIds[i] : 0;
            var ngayDoc = i < ngayDocs.Length ? ngayDocs[i] : null;
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

            var startInfo = await ResolveChiSoDauAsync(
                phongId,
                hopDongId,
                ngayBatDauHopDong,
                dichVuId,
                thang,
                nam,
                current,
                chiSoDauNhap);
            var chiSoDau = startInfo.ChiSoDau;
            var chiSo = new ChiSoDienNuoc
            {
                Id = existId,
                HopDongId = hopDongId,
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
                NgayDoc = ngayDoc?.Date
            };

            items.Add(new ChiSoNhapItem(chiSo));

            if (chiSoDau < 0)
            {
                var dichVu = await dichVuRepo.GetByIdAsync(dichVuId);
                errors.Add($"{dichVu?.TenDichVu ?? $"Dich vu #{dichVuId}"}: chi so dau khong duoc am.");
                continue;
            }

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
        => await chiSoService.LuuBatchAsync(items.Select(x => x.ChiSo));

    private static DateTime ResolveNgayDocMacDinh(int thang, int nam, HopDong hopDong)
    {
        var ngay = new DateTime(nam, thang, DateTime.DaysInMonth(nam, thang));
        if (hopDong.NgayKetThuc.HasValue && hopDong.NgayKetThuc.Value.Date < ngay)
            ngay = hopDong.NgayKetThuc.Value.Date;
        if (hopDong.NgayBatDau.Date > ngay)
            ngay = hopDong.NgayBatDau.Date;
        return ngay;
    }

    private async Task<ChiSoDauInfo> ResolveChiSoDauAsync(
        int phongId,
        int? hopDongId,
        DateTime? ngayBatDauHopDong,
        int dichVuId,
        int thang,
        int nam,
        ChiSoDienNuoc? current,
        decimal? chiSoDauNhap)
    {
        if (current != null)
            return new ChiSoDauInfo(current.ChiSoDau, false, "Đã nhập trong kỳ này");

        var chiSoChuyenDoi = await lichSuHinhThucRepo.GetChiSoDauChuyenDoiAsync(dichVuId, phongId, thang, nam);
        if (chiSoChuyenDoi.HasValue)
            return new ChiSoDauInfo(chiSoChuyenDoi.Value, false, $"Chỉ số đầu chuyển đổi kỳ {thang}/{nam}");

        if (hopDongId.HasValue)
        {
            var opening = await continuity.GetOpeningAsync(hopDongId.Value, dichVuId);
            if (opening != null)
                return new ChiSoDauInfo(opening.EndReading, false, opening.Description);

            var kyTruocCungHopDong = await chiSoRepo.GetChiSoCuoiKyTruocAsync(phongId, dichVuId, thang, nam, hopDongId);
            if (kyTruocCungHopDong != null)
                return new ChiSoDauInfo(kyTruocCungHopDong.ChiSoCuoi, false, $"Kỳ trước cùng hợp đồng #{hopDongId}");
        }

        var cutoffDate = hopDongId.HasValue && ngayBatDauHopDong.HasValue
            ? ngayBatDauHopDong.Value.Date
            : new DateTime(nam, thang, DateTime.DaysInMonth(nam, thang));

        var previous = await continuity.GetPreviousOnOrBeforeAsync(
            phongId, dichVuId, cutoffDate, hopDongId);
        if (previous != null)
            return new ChiSoDauInfo(previous.EndReading, false, previous.Description);

        return new ChiSoDauInfo(chiSoDauNhap ?? 0, true, "Kỳ đầu chưa có dữ liệu cũ");
    }

    private sealed record ChiSoNhapItem(ChiSoDienNuoc ChiSo);
    private sealed record ChiSoValidationResult(List<ChiSoNhapItem> Items, List<string> Errors);
    private sealed record ChiSoDauInfo(decimal ChiSoDau, bool ChoNhapTay, string Nguon);
    private sealed record ChiSoFormData(
        List<DichVu> DichVuTheoChiSo,
        List<ChiSoDienNuoc> ChiSoHienTai,
        Dictionary<int, decimal> ChiSoDauTheoDichVu,
        Dictionary<int, bool> ChoNhapChiSoDauTheoDichVu,
        Dictionary<int, string> NguonChiSoDauTheoDichVu);
}
