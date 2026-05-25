using Microsoft.AspNetCore.Mvc;
using Web_cham_diem.Services;
using Web_cham_diem.ViewModels;

namespace Web_cham_diem.Controllers;

public class RegisterController : Controller
{
    private readonly IAuthenticationService _authService;
    private readonly ILogger<RegisterController> _logger;

    public RegisterController(IAuthenticationService authService, ILogger<RegisterController> logger)
    {
        _authService = authService;
        _logger = logger;
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

        if (!model.AcceptTerms)
        {
            ModelState.AddModelError(nameof(model.AcceptTerms), "Bạn phải chấp nhận điều khoản sử dụng.");
            return View(model);
        }

        var result = await _authService.RegisterAsync(model);
        var (success, message) = result;

        if (success)
        {
            TempData["SuccessMessage"] = message;
            return RedirectToAction("Index", "Login");
        }

        ModelState.AddModelError("", message);
        return View(model);
    }
}