using Microsoft.AspNetCore.Authorization; // Thêm thư viện này
using Microsoft.AspNetCore.Mvc;

namespace Web_cham_diem.Controllers
{
    [Authorize] // <--- BẮT BUỘC: Chặn người dùng chưa đăng nhập truy cập vào đây
    public class AccountController : Controller
    {
        // Đường dẫn truy cập: /Account/User
        public IActionResult User()
        {
            return View();
        }

        // Đường dẫn truy cập: /Account/Admin
        public IActionResult Admin()
        {
            return View();
        }
    }
}