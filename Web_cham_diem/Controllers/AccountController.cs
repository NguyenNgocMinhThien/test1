using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web_cham_diem.Models;
using Web_cham_diem.Models.ViewModels;

namespace Web_cham_diem.Controllers;

[Authorize] // BẮT BUỘC: Phải đăng nhập mới được vào bất kỳ tính năng nào trong này
public class AccountController : Controller
{
    private readonly ApplicationDbContext _context;

    // Sử dụng Dependency Injection để truyền Database Context vào Controller
    public AccountController(ApplicationDbContext context)
    {
        _context = context;
    }

    // ==========================================
    // KHÔNG GIAN DÀNH CHO USER / STUDENT
    // ==========================================

    // Đường dẫn truy cập: /Account/User
    [Authorize(Roles = "Student,Admin")]
    public IActionResult User()
    {
        return View();
    }

    // ==========================================
    // KHÔNG GIAN QUẢN TRỊ VIÊN (ADMIN ONLY)
    // ==========================================

    // Đường dẫn: /Account/Admin
    [Authorize(Roles = "Admin")]
    public IActionResult Admin()
    {
        return View();
    }

    // Đường dẫn: /Account/UserManagement
    [Authorize(Roles = "Admin")]
    public IActionResult UserManagement()
    {
        return View();
    }

    // Đường dẫn xử lý API tạo người dùng từ AJAX Form gửi lên
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateUser(CreateUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(new { success = false, message = "Dữ liệu gửi lên không đúng định dạng hoặc thiếu thông tin bắt buộc." });
        }

        try
        {
            var emailLower = model.Email.ToLower().Trim();
            var studentCodeUpper = model.StudentId.ToUpper().Trim();

            // 1. Kiểm tra xem Email đã có ai đăng ký trước đó chưa
            bool isEmailExist = await _context.Users.AnyAsync(u => u.Email.ToLower() == emailLower);
            if (isEmailExist)
            {
                return Json(new { success = false, message = "Email này đã được sử dụng trong hệ thống." });
            }

            // 2. Kiểm tra xem MSSV / Mã nhân viên có bị trùng lặp không
            bool isStudentIdExist = await _context.Users.AnyAsync(u => u.StudentId != null && u.StudentId.ToUpper() == studentCodeUpper);
            if (isStudentIdExist)
            {
                return Json(new { success = false, message = "Mã số sinh viên/nhân viên này đã tồn tại." });
            }

            // 3. Khởi tạo tài khoản mới và mã hóa mật khẩu bằng BCrypt
            var newUser = new Users
            {
                FullName = model.FullName.Trim(),
                Email = emailLower,
                StudentId = studentCodeUpper,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password), // Mã hóa chuỗi mật khẩu (mặc định: UEF@12345)
                PhoneNumber = string.Empty,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync(); // Lưu dữ liệu để sinh ra UserId tự động từ PostgreSQL

            // 4. Tìm kiếm ID tương ứng của vai trò hệ thống được chọn từ bảng Roles
            var selectedRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.RoleName.ToLower() == model.RoleName.ToLower());

            if (selectedRole == null)
            {
                return Json(new { success = false, message = $"Không tìm thấy vai trò '{model.RoleName}' cấu hình trong hệ thống." });
            }

            // 5. Gán bản ghi liên kết vào bảng trung gian UserRoles
            var userRoleLink = new UserRoles
            {
                UserId = newUser.UserId,       // Lấy UserId vừa tạo ở bước trên
                RoleId = selectedRole.RoleId,   // Lấy RoleId khớp từ bảng Roles
                AssignedAt = DateTime.UtcNow
            };

            _context.UserRoles.Add(userRoleLink);
            await _context.SaveChangesAsync(); // Hoàn tất giao dịch ghi dữ liệu liên kết

            return Json(new { success = true, message = "Đã tạo tài khoản mới và phân quyền thành công!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lỗi hệ thống phát sinh: " + ex.Message });
        }
    }
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetUserList(string search, string role, string status, string department)
    {
        // 1. Khởi tạo câu lệnh truy vấn LINQ (Chưa thực thi xuống database)
        var query = _context.Users.AsQueryable();

        // 2. LỌC THEO TỪ KHÓA TÌM KIẾM (Tên, Email hoặc MSSV)
        if (!string.IsNullOrEmpty(search))
        {
            var searchLower = search.ToLower().Trim();
            query = query.Where(u => u.FullName.ToLower().Contains(searchLower) ||
                                     u.Email.ToLower().Contains(searchLower) ||
                                     (u.StudentId != null && u.StudentId.ToLower().Contains(searchLower)));
        }

        // 3. LỌC THEO TRẠNG THÁI (Active / Locked)
        if (!string.IsNullOrEmpty(status))
        {
            if (status.Equals("Active", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(u => u.IsActive == true);
            }
            else if (status.Equals("Locked", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(u => u.IsActive == false);
            }
        }

        // 4. LỌC THEO KHOA / PHÒNG BAN
        // (Nếu sau này bạn có thêm thuộc tính Department vào thực thể Users thì bỏ comment dòng dưới nhé)
        /*
        if (!string.IsNullOrEmpty(department))
        {
            query = query.Where(u => u.Department == department);
        }
        */

        // 5. CHUYỂN ĐỔI DỮ LIỆU SANG ĐỊNH DẠNG JSON ĐỂ TRẢ VỀ CHO FRONTEND
        var userList = await query
            .Select(u => new {
                u.UserId,
                u.FullName,
                u.Email,
                u.StudentId,
                u.IsActive,
                CreatedAt = u.CreatedAt.ToString("dd/MM/yyyy"),
                // Lấy tên vai trò từ bảng liên kết trung gian
                RoleName = _context.UserRoles
                    .Where(ur => ur.UserId == u.UserId)
                    .Select(ur => ur.Role.RoleName)
                    .FirstOrDefault() ?? "Chưa phân quyền"
            })
            .ToListAsync();

        // 6. LỌC THEO VAI TRÒ (Vì RoleName nằm ở bảng liên kết nên lọc trên bộ nhớ sau khi Select)
        if (!string.IsNullOrEmpty(role))
        {
            userList = userList.Where(u => u.RoleName.Equals(role, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        // 7. SẮP XẾP: Tài khoản mới tạo lên đầu
        var result = userList.OrderByDescending(u => u.UserId).ToList();

        return Json(result);
    }
    // ==========================================
    // BỔ SUNG CÁC TÍNH NĂNG QUẢN LÝ TÀI KHOẢN nâng cao
    // ==========================================

    // 1. API Lấy thông tin chi tiết một User (Dùng chung cho cả Xem chi tiết & Sửa thông tin)
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetUserDetail(int id)
    {
        var user = await _context.Users
            .Select(u => new
            {
                u.UserId,
                u.FullName,
                u.Email,
                u.StudentId,
                u.IsActive,
                // (Nếu database chưa có trường Department/Khoa, tạm thời trả về string trống hoặc giá trị mẫu)
                Department = "Công nghệ thông tin",
                RoleName = _context.UserRoles
                    .Where(ur => ur.UserId == u.UserId)
                    .Select(ur => ur.Role.RoleName)
                    .FirstOrDefault() ?? "Student"
            })
            .FirstOrDefaultAsync(u => u.UserId == id);

        if (user == null)
        {
            return Json(new { success = false, message = "Không tìm thấy người dùng này." });
        }

        return Json(new { success = true, data = user });
    }

    // 2. API Cập nhật thông tin (Họ tên, MSSV, Khoa...) - Không sửa Email
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateUser(int userId, string fullName, string studentId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return Json(new { success = false, message = "Người dùng không tồn tại." });

            if (string.IsNullOrEmpty(fullName)) return Json(new { success = false, message = "Họ tên không được để trống." });

            // Kiểm tra trùng mã MSSV/Mã NV với người khác
            if (!string.IsNullOrEmpty(studentId))
            {
                var studentIdUpper = studentId.ToUpper().Trim();
                bool isIdExist = await _context.Users.AnyAsync(u => u.UserId != userId && u.StudentId != null && u.StudentId.ToUpper() == studentIdUpper);
                if (isIdExist) return Json(new { success = false, message = "Mã số sinh viên/nhân viên này đã thuộc về tài khoản khác." });
                user.StudentId = studentIdUpper;
            }

            user.FullName = fullName.Trim();
            // user.Department = department; // Bỏ comment nếu DB của bạn đã migration thêm cột này

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Cập nhật thông tin tài khoản thành công!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
        }
    }

    // 3. API Thay đổi vai trò / Phân quyền (Bảng trung gian UserRoles)
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateUserRole(int userId, string roleName)
    {
        try
        {
            var userExists = await _context.Users.AnyAsync(u => u.UserId == userId);
            if (!userExists) return Json(new { success = false, message = "Người dùng không tồn tại." });

            // Tìm kiếm RoleID mới dựa vào tên quyền lựa chọn công cụ hiển thị
            var selectedRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName.ToLower() == roleName.ToLower());
            if (selectedRole == null) return Json(new { success = false, message = "Quyền hệ thống yêu cầu không hợp lệ." });

            // Tìm bản ghi phân quyền cũ của User này (nếu có)
            var currentLink = await _context.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == userId);

            if (currentLink != null)
            {
                // Xóa quyền cũ đi
                _context.UserRoles.Remove(currentLink);
                await _context.SaveChangesAsync();
            }

            // Tạo quyền mới tương ứng vào bảng liên kết
            var newLink = new UserRoles
            {
                UserId = userId,
                RoleId = selectedRole.RoleId,
                AssignedAt = DateTime.UtcNow
            };

            _context.UserRoles.Add(newLink);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã cập nhật phân quyền mới thành công!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lỗi hệ thống phân quyền: " + ex.Message });
        }
    }

    // 4. API Khóa / Mở khóa tài khoản (Bật tắt IsActive)
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ToggleUserStatus(int id)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return Json(new { success = false, message = "Không tìm thấy tài khoản cần xử lý." });

            // Đảo trạng thái: Nếu đang True -> False (Khóa), Nếu đang False -> True (Mở khóa)
            user.IsActive = !user.IsActive;

            await _context.SaveChangesAsync();

            string msg = user.IsActive ? "Đã mở khóa tài khoản thành công!" : "Đã khóa tài khoản thành công!";
            return Json(new { success = true, message = msg, isActive = user.IsActive });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Thao tác thất bại: " + ex.Message });
        }
    }

    // Thêm action cho Cài đặt hệ thống
    [Authorize(Roles = "Admin")]
    public IActionResult SystemSettings()
    {
        return View();
    }

    // Thêm action cho Báo cáo và thống kê
    [Authorize(Roles = "Admin")]
    public IActionResult Reports()
    {
        return View();
    }

    // Thêm action cho Tổng quan cuộc thi
    [Authorize(Roles = "Admin")]
    public IActionResult ContestOverview()
    {
        return View();
    }

    // Thêm action Thông báo hệ thống
    [Authorize(Roles = "Admin")]
    public IActionResult SystemNotifications()
    {
        return View();
    }

    // Thêm action Nhật ký hoạt động
    [Authorize(Roles = "Admin")]
    public IActionResult ActivityLogs()
    {
        return View();
    }
}