using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Web_cham_diem.Services;
using Web_cham_diem.ViewModels;
using AuthService = Web_cham_diem.Services.IAuthenticationService;

namespace Web_cham_diem.Controllers;

public class LoginController : Controller
{
    private readonly AuthService _authService;
    private readonly ILogger<LoginController> _logger;

    public LoginController(AuthService authService, ILogger<LoginController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index()
    {
        // Nếu đã đăng nhập thì redirect về home
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _authService.LoginAsync(model);
        var (success, user, message) = result;

        if (success && user != null)
        {
            // Tạo claims cho user
            var claims = new List<Claim>
{
    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
    new Claim(ClaimTypes.Email, user.Email),
    new Claim(ClaimTypes.Name, user.FullName),
    new Claim("StudentId", user.StudentId ?? string.Empty),
    new Claim("PhoneNumber", user.PhoneNumber ?? string.Empty)
};

            foreach (var roleName in user.UserRoles.Select(x => x.Role.RoleName).Distinct())
            {
                claims.Add(new Claim(ClaimTypes.Role, roleName));
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(model.RememberMe ? 30 : 1)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties
            );

            _logger.LogInformation($"Người dùng {user.Email} đăng nhập thành công.");
            TempData["SuccessMessage"] = $"Chào mừng {user.FullName}! Đăng nhập thành công.";
            return RedirectToAction("Index", "Home");
        }

        ModelState.AddModelError("", message ?? "Email hoặc mật khẩu không đúng.");
        _logger.LogWarning($"Đăng nhập thất bại: {message}");
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        _logger.LogInformation("Người dùng đã đăng xuất.");
        TempData["SuccessMessage"] = "Đã đăng xuất thành công.";
        return RedirectToAction("Index", "Home");
    }
}