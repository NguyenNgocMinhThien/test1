using Microsoft.AspNetCore.Mvc;
using Web_cham_diem.Services;
using Web_cham_diem.ViewModels;

namespace Web_cham_diem.Controllers;

public class RegisterController : Controller
{
    private readonly IAuthenticationService _authService;

    public RegisterController(IAuthenticationService authService)
    {
        _authService = authService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _authService.RegisterAsync(model);
        if (result)
        {
            return RedirectToAction("Index", "Login");
        }

        ModelState.AddModelError("", "Email đã được đăng ký hoặc đã có lỗi");
        return View(model);
    }
}