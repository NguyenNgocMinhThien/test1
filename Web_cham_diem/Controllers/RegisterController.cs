using Microsoft.AspNetCore.Mvc;
using Web_cham_diem.Models;

namespace Web_cham_diem.Controllers;

public class RegisterController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Index(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        return RedirectToAction("Index", "Login");
    }
}
