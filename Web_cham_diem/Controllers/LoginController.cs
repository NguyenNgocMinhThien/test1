using Microsoft.AspNetCore.Mvc;
using Web_cham_diem.Services;
using Web_cham_diem.ViewModels;

namespace Web_cham_diem.Controllers;

public class LoginController : Controller
{
    private readonly IAuthenticationService _authService;

    public LoginController(IAuthenticationService authService)
    {
        _authService = authService;
    }

    [HttpGet]
    public IActionResult Index()
    {
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
        if (result)
        {
            return RedirectToAction("Index", "Home");
        }

        ModelState.AddModelError("", "Email hoặc mật khẩu không đúng");
        return View(model);
    }
}