using Microsoft.AspNetCore.Mvc;
using Web_cham_diem.Models;

namespace Web_cham_diem.Controllers;

public class LoginController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Index(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        return RedirectToAction("Index", "Home");
    }
}
