using Microsoft.AspNetCore.Mvc;

namespace Web_cham_diem.Controllers
{
    public class OrganizerController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        // 1. Dashboard - Tổng quan chung
        public IActionResult Dashboard()
        {
            return View();
        }

        // 2. Quản lý cuộc thi (Tạo mới, sửa, xóa cuộc thi)
        public IActionResult Contests()
        {
            return View();
        }

        // 3. Quản lý đăng ký & Bài dự thi (Duyệt hồ sơ, thu bài)
        public IActionResult Submissions()
        {
            return View();
        }

        // 4. Quản lý chấm điểm (Phân công giám khảo, theo dõi điểm)
        public IActionResult Grading()
        {
            return View();
        }

        // 5. Kết quả & Báo cáo (Tổng hợp điểm, xuất file)
        public IActionResult Results()
        {
            return View();
        }

        // 6. Thông báo cuộc thi (Nhắn tin, nhắc nhở deadline)
        public IActionResult Notifications()
        {
            return View();
        }

        // 7. Theo dõi tiến độ (Tiến trình chung)
        public IActionResult Progress()
        {
            return View();
        }
    }
}
