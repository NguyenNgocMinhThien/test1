using Microsoft.AspNetCore.Mvc;

namespace Web_cham_diem.Controllers
{
    public class AccountController : Controller
    {
        // Đường dẫn truy cập sẽ là: /Account/Admin
        public IActionResult Admin()
        {
            return View();
        }
    }
}