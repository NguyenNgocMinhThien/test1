using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Web_cham_diem.Models;

namespace Web_cham_diem.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class NotificationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NotificationController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // 1. Đếm tổng số lượng Broadcast đã gửi từ DB thực tế
            int totalBroadcasts = await _context.Notifications.CountAsync();

            // 2. Đếm số lượng thông báo thuộc dạng Bảo trì / Cập nhật hệ thống
            int totalMaintenance = await _context.Notifications
                .Where(n => n.Title.Contains("Bảo trì") || n.Title.Contains("Cập nhật"))
                .CountAsync();

            // 3. Giả lập số lượng Cảnh báo bảo mật (Giữ nguyên logic giao diện của bạn)
            int securityAlerts = 3;

            // 4. Tính tỷ lệ đã đọc (%) dựa trên cột IsRead thực tế
            int totalNotifications = await _context.Notifications.CountAsync();
            int readNotifications = await _context.Notifications.Where(n => n.IsRead == true).CountAsync();

            int readRate = totalNotifications > 0
                ? (int)((double)readNotifications / totalNotifications * 100)
                : 100; // Mặc định là 100% nếu database chưa có thông báo nào

            // Gán dữ liệu thống kê vào ViewData để truyền sang View hiển thị lên các Card
            ViewData["TotalBroadcasts"] = totalBroadcasts.ToString("N0");
            ViewData["TotalMaintenance"] = totalMaintenance.ToString();
            ViewData["SecurityAlerts"] = securityAlerts.ToString();
            ViewData["ReadRate"] = readRate + "%";

            // 5. Lấy danh sách thông báo thực tế xếp từ mới nhất đến cũ nhất
            var notificationsList = await _context.Notifications
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            // Truyền danh sách thông báo sang View
            return View(notificationsList);
        }
    }
}