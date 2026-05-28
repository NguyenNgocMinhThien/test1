using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;
using Web_cham_diem.Services;
using Web_cham_diem.ViewModels;
using AuthService = Web_cham_diem.Services.IAuthenticationService;

namespace Web_cham_diem.Controllers;

[AllowAnonymous]
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
        // Nếu người dùng đã đăng nhập trước đó rồi thì điều hướng thông minh dựa vào Role của họ
        if (User.Identity?.IsAuthenticated == true)
        {
            if (User.IsInRole("Admin"))
                return RedirectToAction("Admin", "Account");

            return RedirectToAction("User", "Account");
        }

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

            var claims = new List<Claim>
{
    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
    new Claim(ClaimTypes.Email, user.Email),
    new Claim(ClaimTypes.Name, user.FullName),
    new Claim("StudentId", user.StudentId ?? string.Empty),
    new Claim("PhoneNumber", user.PhoneNumber ?? string.Empty)
};

            var roleNames = user.UserRoles?
                .Select(ur => ur.Role?.RoleName)
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Distinct()
                .ToList() ?? new List<string>();

            if (!roleNames.Any())
            {
                roleNames.Add("Student");
            }

            foreach (var role in roleNames)
            {
                claims.Add(new Claim(ClaimTypes.Role, role!));
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

            _logger.LogInformation($"Người dùng {user.Email} đăng nhập thành công với quyền: {string.Join(", ", roleNames)}.");
            TempData["SuccessMessage"] = $"Chào mừng {user.FullName}! Đăng nhập thành công.";

            // THAY ĐỔI QUAN TRỌNG: Phân luồng điều hướng khi đăng nhập thành công
            if (roleNames.Contains("Admin"))
            {
                return RedirectToAction("Admin", "Account"); // Đẩy tài khoản Admin vào trang quản trị
            }
            else
            {
                return RedirectToAction("User", "Account");  // Đẩy tài khoản sinh viên vào trang cá nhân
            }
        }

        ModelState.AddModelError(string.Empty, message ?? "Email hoặc mật khẩu không đúng.");
        _logger.LogWarning($"Đăng nhập thất bại: {message}");
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        _logger.LogInformation("Người dùng đã đăng xuất.");
        TempData["SuccessMessage"] = "Đã đăng xuất thành công.";
        return RedirectToAction("Index", "Login");
    }
}