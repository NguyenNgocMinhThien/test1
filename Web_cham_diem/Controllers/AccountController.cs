using Microsoft.AspNetCore.Authorization; // Thêm thư viện này
using Microsoft.AspNetCore.Mvc;

namespace Web_cham_diem.Controllers
{
    [Authorize] // <--- BẮT BUỘC: Chặn người dùng chưa đăng nhập truy cập vào đây
    public class AccountController : Controller
    {
        // Đường dẫn truy cập: /Account/User
        [Authorize(Roles = "Student,Admin")]
        public IActionResult User()
        {
            return View();
        }

        // Đường dẫn: /Account/Admin -> KHÓA CHẶT: Chỉ duy nhất tài khoản có quyền Admin mới vào được
        [Authorize(Roles = "Admin")]
        public IActionResult Admin()
        {
            return View();
        }

        // Path: /Account/UserManagement
        public IActionResult UserManagement()
        {
            // Ở giai đoạn này chúng ta trả về View với dữ liệu trống (mock data đã có sẵn trong HTML)
            return View();
        }

        // Thêm action cho Cài đặt hệ thống
        public IActionResult SystemSettings()
        {
            return View();
        }

        // Thêm action cho Báo cáo và thống kê
        public IActionResult Reports()
        {
            return View();
        }

        // Thêm action cho Tổng quan cuộc thi
        public IActionResult ContestOverview()
        {
            return View();
        }
        // Thêm action Thông báo hệ thống
        public IActionResult SystemNotifications()
        {
            return View();
        }

        // Thêm action Nhật ký hoạt động
        public IActionResult ActivityLogs()
        {
            return View();
        }

       
    }
}