using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyNhaTro.Models;
using QuanLyNhaTro.Services;

namespace QuanLyNhaTro.Controllers;

public sealed class AccountController(AdminCredentialService credentials) : Controller
{
    [AllowAnonymous, HttpGet]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Username)
            || string.IsNullOrEmpty(model.Password)
            || !credentials.Verify(model.Username, model.Password))
        {
            ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng.");
            model.Password = string.Empty;
            ModelState.Remove(nameof(model.Password));
            return View(model);
        }
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, model.Username.Trim()), new Claim(ClaimTypes.Role, "Administrator")],
            CookieAuthenticationDefaults.AuthenticationScheme);
        var authenticationProperties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            AllowRefresh = true
        };
        if (model.RememberMe)
            authenticationProperties.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(365);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            authenticationProperties);
        return LocalRedirect(Url.IsLocalUrl(model.ReturnUrl)
            ? model.ReturnUrl!
            : Url.Action("Index", "Home")!);
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();
}
