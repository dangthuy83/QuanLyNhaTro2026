using Microsoft.AspNetCore.Mvc;
using QuanLyNhaTro.Models;
using QuanLyNhaTro.Services;

namespace QuanLyNhaTro.Controllers;

public class CutoverController(DongHopDongTruocCutoverService service) : Controller
{
    [HttpGet]
    public async Task<IActionResult> DongHopDongTruocCutover(int hopDongId)
    {
        ViewData["ActiveMenu"] = "hopdong";
        try
        {
            return View(await service.TaoFormAsync(hopDongId));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DongHopDongTruocCutover(
        DongHopDongTruocCutoverViewModel model)
    {
        ViewData["ActiveMenu"] = "hopdong";
        if (!ModelState.IsValid) return View(model);
        try
        {
            await service.ThucHienAsync(model, User.Identity?.Name ?? "Administrator");
            TempData["Success"] = "Đã đóng hợp đồng trước cutover và ghi audit.";
            return RedirectToAction("Details", "HopDong", new { id = model.HopDongId });
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }
}
