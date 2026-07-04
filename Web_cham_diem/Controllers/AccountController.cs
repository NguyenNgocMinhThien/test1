using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Web_cham_diem.Models;
using Web_cham_diem.Models.ViewModels;
using Web_cham_diem.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace Web_cham_diem.Controllers;

[Authorize]
public class AccountController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationsService _notificationsService;

    public AccountController(ApplicationDbContext context, INotificationsService notificationsService)
    {
        _context = context;
        _notificationsService = notificationsService;
    }

    // ==========================================
    // KHÔNG GIAN ĐĂNG NHẬP / ĐĂNG XUẤT (CÔNG KHAI)
    // ==========================================

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login()
    {
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            if (User.IsInRole("Admin")) return RedirectToAction("Admin");
            return RedirectToAction("UserDashboard");
        }
        return View();
    }

    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
        {
            return Json(new { success = false, message = "Vui lòng nhập đầy đủ Email và Mật khẩu." });
        }

        try
        {
            var emailTrim = request.Email.ToLower().Trim();
            // Tìm user dựa trên Email từ DB
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == emailTrim);

            if (user == null)
            {
                return Json(new { success = false, message = "Tài khoản Email không tồn tại trên hệ thống." });
            }

            if (user.IsActive == false)
            {
                return Json(new { success = false, message = "Tài khoản của bạn hiện đang bị khóa." });
            }

            // 🔥 SỬA LỖI TẠI ĐÂY: Chỉ dùng duy nhất trường dữ liệu chuẩn u.PasswordHash từ Model của bạn
            string dbPasswordHash = user.PasswordHash;

            if (string.IsNullOrEmpty(dbPasswordHash))
            {
                return Json(new { success = false, message = "Tài khoản này chưa được cấu hình mật khẩu băm hợp lệ." });
            }

            // Tiến hành đối chiếu chuỗi mã hóa BCrypt
            bool isValidPassword = false;
            try
            {
                isValidPassword = BCrypt.Net.BCrypt.Verify(request.Password, dbPasswordHash);
            }
            catch
            {
                // Phòng trường hợp dữ liệu cũ lưu dạng text thuần không băm bằng BCrypt
                isValidPassword = (request.Password == dbPasswordHash);
            }

            if (!isValidPassword)
            {
                return Json(new { success = false, message = "Mật khẩu nhập vào không chính xác." });
            }

            // Lấy tên quyền (Role) của User từ DB để nạp vào hệ thống Authentication Cookie
            var roleName = await _context.UserRoles
                .Where(ur => ur.UserId == user.UserId)
                .Select(ur => ur.Role.RoleName)
                .FirstOrDefaultAsync() ?? "Student";

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.FullName ?? ""),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Role, roleName)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties { IsPersistent = true };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

            return Json(new { success = true, message = "Đăng nhập thành công!", role = roleName });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lỗi hệ thống đăng nhập: " + ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    // ==========================================
    // KHÔNG GIAN DÀNH CHO USER / STUDENT
    // ==========================================

    [Authorize(Roles = "Student,Admin")]
    public IActionResult UserDashboard()
    {
        return View();
    }

    // ==========================================
    // KHÔNG GIAN QUẢN TRỊ VIÊN (ADMIN ONLY)
    // ==========================================

    [Authorize(Roles = "Admin")]
    public IActionResult Admin()
    {
        return View();
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAdminMainDashboardData()
    {
        try
        {
            var totalUsers = await _context.Users.CountAsync();

            var totalStudents = await _context.UserRoles.CountAsync(ur => ur.Role.RoleName == "Student");
            var totalLecturers = await _context.UserRoles.CountAsync(ur => ur.Role.RoleName == "Lecturer" || ur.Role.RoleName == "Giảng viên");
            var totalJudges = await _context.UserRoles.CountAsync(ur => ur.Role.RoleName == "Judge" || ur.Role.RoleName == "Ban giám khảo");
            var totalAdmins = await _context.UserRoles.CountAsync(ur => ur.Role.RoleName == "Admin");

            var totalContests = await _context.Competitions.CountAsync();
            var activeContests = await _context.Competitions.CountAsync(c => c.Status == "Đang hoạt động" || c.Status == "Active");

            var totalSubmissions = await _context.Submissions.CountAsync();

            var currentYear = DateTime.UtcNow.Year;
            var barLabels = new string[5];
            var barData = new int[5];

            for (int i = 0; i < 5; i++)
            {
                int targetYear = currentYear - (4 - i);
                barLabels[i] = "Năm " + targetYear;
                barData[i] = await _context.Competitions.CountAsync(c => c.CreatedAt.Year == targetYear);
            }

            return Json(new
            {
                success = true,
                stats = new
                {
                    TotalUsers = totalUsers,
                    TotalStudents = totalStudents,
                    TotalLecturers = totalLecturers,
                    TotalContests = totalContests,
                    ActiveContests = activeContests,
                    TotalSubmissions = totalSubmissions
                },
                pieChart = new
                {
                    Labels = new[] { "Sinh viên", "Giảng viên", "Giám khảo", "Quản trị viên" },
                    Data = new[] { totalStudents, totalLecturers, totalJudges, totalAdmins }
                },
                barChart = new
                {
                    Labels = barLabels,
                    Data = barData
                }
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lỗi tải số liệu hệ thống: " + ex.Message });
        }
    }

    [Authorize(Roles = "Admin")]
    public IActionResult UserManagement()
    {
        return View();
    }

    [Authorize(Roles = "Admin")]
    public IActionResult ContestOverview()
    {
        return View();
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetRealContestOverviewData(string search, string status, string field, string schoolYear)
    {
        try
        {
            var competitionQuery = _context.Competitions.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                var searchLower = search.ToLower().Trim();
                competitionQuery = competitionQuery.Where(c => c.CompetitionName.ToLower().Contains(searchLower) ||
                                                               (c.Description != null && c.Description.ToLower().Contains(searchLower)));
            }

            if (!string.IsNullOrEmpty(status))
            {
                competitionQuery = competitionQuery.Where(c => c.Status == status);
            }

            var rawCompetitions = await competitionQuery.OrderByDescending(c => c.CreatedAt).ToListAsync();

            var totalCompetitions = await _context.Competitions.CountAsync();
            var activeCompetitions = await _context.Competitions.CountAsync(c => c.Status == "Đang hoạt động" || c.Status == "Active");
            var upcomingCompetitions = await _context.Competitions.CountAsync(c => c.Status == "Sắp diễn ra" || c.Status == "Upcoming");
            var completedCompetitions = await _context.Competitions.CountAsync(c => c.Status == "Đã kết thúc" || c.Status == "Completed");

            var totalRegistrations = await _context.Registrations.CountAsync();
            var totalSubmissions = await _context.Submissions.CountAsync();

            var stats = new
            {
                TotalContests = totalCompetitions,
                ActiveContests = activeCompetitions,
                UpcomingContests = upcomingCompetitions,
                CompletedContests = completedCompetitions,
                TotalRegistrations = totalRegistrations >= 1000 ? (totalRegistrations / 1000.0).ToString("0.0") + "K" : totalRegistrations.ToString(),
                TotalSubmissions = totalSubmissions >= 1000 ? (totalSubmissions / 1000.0).ToString("0.0") + "K" : totalSubmissions.ToString()
            };

            var statusGroup = await _context.Competitions
                .GroupBy(c => c.Status)
                .Select(g => new { StatusName = g.Key ?? "Chưa rõ", Count = g.Count() })
                .ToListAsync();

            var fieldData = new
            {
                Labels = statusGroup.Select(f => f.StatusName).ToArray(),
                Data = statusGroup.Select(f => f.Count).ToArray()
            };

            var monthLabels = new string[6];
            var newContestCounts = new int[6];
            var submissionCounts = new int[6];

            for (int i = 0; i < 6; i++)
            {
                var targetDate = DateTime.UtcNow.AddMonths(-i);
                monthLabels[5 - i] = $"Tháng {targetDate.Month}";

                newContestCounts[5 - i] = await _context.Competitions
                    .CountAsync(c => c.CreatedAt.Month == targetDate.Month && c.CreatedAt.Year == targetDate.Year);

                submissionCounts[5 - i] = await _context.Submissions
                    .CountAsync(s => s.SubmissionDate.Month == targetDate.Month && s.SubmissionDate.Year == targetDate.Year);
            }

            var monthlyData = new
            {
                Labels = monthLabels,
                NewContests = newContestCounts,
                Submissions = submissionCounts
            };

            var monitoringList = rawCompetitions.Select(c => {
                double progressPercent = 0;
                var totalTicks = (c.EndDate - c.StartDate).TotalSeconds;
                var elapsedTicks = (DateTime.UtcNow - c.StartDate).TotalSeconds;

                if (totalTicks > 0 && elapsedTicks > 0)
                {
                    progressPercent = Math.Min(100, Math.Round((elapsedTicks / totalTicks) * 100));
                }
                if (DateTime.UtcNow > c.EndDate) progressPercent = 100;

                return new
                {
                    Id = c.CompetitionId,
                    Name = c.CompetitionName,
                    Code = "COMP-" + c.CompetitionId,
                    Field = "Tổng hợp",
                    Status = c.Status ?? "Đang diễn ra",
                    Progress = progressPercent,
                    Registrations = _context.Registrations.Count(r => r.CompetitionId == c.CompetitionId),
                    Submissions = _context.Submissions.Count(s => s.CompetitionId == c.CompetitionId),
                    EndDate = c.EndDate != DateTime.MinValue ? c.EndDate.ToString("dd/MM/yyyy") : "Vô thời hạn"
                };
            }).ToList();

            return Json(new
            {
                success = true,
                stats = stats,
                monthlyData = monthlyData,
                fieldData = fieldData,
                contests = monitoringList
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lỗi kết nối cơ sở dữ liệu: " + ex.Message });
        }
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetUserList(string search, string role, string status, string department)
    {
        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            var searchLower = search.ToLower().Trim();
            query = query.Where(u => u.FullName.ToLower().Contains(searchLower) ||
                                 u.Email.ToLower().Contains(searchLower) ||
                                 (u.StudentId != null && u.StudentId.ToLower().Contains(searchLower)));
        }

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

        var userList = await query
            .Select(u => new {
                u.UserId,
                u.FullName,
                u.Email,
                u.StudentId,
                u.IsActive,
                CreatedAt = u.CreatedAt.ToString("dd/MM/yyyy"),
                RoleName = _context.UserRoles
                    .Where(ur => ur.UserId == u.UserId)
                    .Select(ur => ur.Role.RoleName)
                    .FirstOrDefault() ?? "Chưa phân quyền"
            })
            .ToListAsync();

        if (!string.IsNullOrEmpty(role))
        {
            userList = userList.Where(u => u.RoleName.Equals(role, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return Json(userList.OrderByDescending(u => u.UserId).ToList());
    }

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
                Department = "Công nghệ thông tin",
                RoleName = _context.UserRoles
                    .Where(ur => ur.UserId == u.UserId)
                    .Select(ur => ur.Role.RoleName)
                    .FirstOrDefault() ?? "Student"
            })
            .FirstOrDefaultAsync(u => u.UserId == id);

        if (user == null) return Json(new { success = false, message = "Không tìm thấy người dùng này." });
        return Json(new { success = true, data = user });
    }

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

            if (!string.IsNullOrEmpty(studentId))
            {
                var studentIdUpper = studentId.ToUpper().Trim();
                bool isIdExist = await _context.Users.AnyAsync(u => u.UserId != userId && u.StudentId != null && u.StudentId.ToUpper() == studentIdUpper);
                if (isIdExist) return Json(new { success = false, message = "Mã số sinh viên/nhân viên này đã thuộc về tài khoản khác." });
                user.StudentId = studentIdUpper;
            }

            user.FullName = fullName.Trim();
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Cập nhật thông tin tài khoản thành công!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateUserRole(int userId, string roleName)
    {
        try
        {
            var userExists = await _context.Users.AnyAsync(u => u.UserId == userId);
            if (!userExists) return Json(new { success = false, message = "Người dùng không tồn tại." });

            var selectedRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName.ToLower() == roleName.ToLower());
            if (selectedRole == null) return Json(new { success = false, message = "Quyền hệ thống yêu cầu không hợp lệ." });

            var currentLink = await _context.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == userId);
            if (currentLink != null)
            {
                _context.UserRoles.Remove(currentLink);
                await _context.SaveChangesAsync();
            }

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

    public class CreateUserRequest
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }

    }
    // 1. Tạo một class nhỏ này ngay trên đầu hoặc cùng file với Controller để nhận trọn gói Form dữ liệu
    public class AdminCreateUserForm
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? StudentId { get; set; }
        public string? RoleName { get; set; }
        public string? Password { get; set; } // Nhận mật khẩu Admin tự nhập từ giao diện
    }

    // 2. Hàm xử lý chính thức
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateUser([FromForm] AdminCreateUserForm model) // Nhận dạng đối tượng giúp xử lý triệt để lỗi 415
    {
        if (string.IsNullOrEmpty(model.FullName) || string.IsNullOrEmpty(model.Email))
        {
            return Json(new { success = false, message = "Họ tên và Email không được để trống." });
        }

        try
        {
            var emailTrim = model.Email.ToLower().Trim();

            // Kiểm tra trùng Email
            bool isEmailExist = await _context.Users.AnyAsync(u => u.Email.ToLower() == emailTrim);
            if (isEmailExist)
            {
                return Json(new { success = false, message = $"Email '{model.Email}' đã được sử dụng bởi một tài khoản khác!" });
            }

            string? studentIdUpper = null;
            if (!string.IsNullOrEmpty(model.StudentId))
            {
                studentIdUpper = model.StudentId.ToUpper().Trim();
                // Kiểm tra trùng mã số
                bool isIdExist = await _context.Users.AnyAsync(u => u.StudentId != null && u.StudentId.ToUpper() == studentIdUpper);
                if (isIdExist)
                {
                    return Json(new { success = false, message = $"Mã số sinh viên/nhân viên '{studentIdUpper}' đã tồn tại từ trước." });
                }
            }

            // --- ĐÚNG YÊU CẦU CỦA BẠN: LẤY MẬT KHẨU ADMIN ĐƯA CHO ---
            // Nếu trên giao diện Admin điền mật khẩu thì lấy, nếu để trống hoàn toàn thì dùng "UEF@12345"
            var actualPassword = string.IsNullOrEmpty(model.Password) ? "UEF@12345" : model.Password;
            string finalHash = BCrypt.Net.BCrypt.HashPassword(actualPassword);

            var newUser = new Users
            {
                FullName = model.FullName.Trim(),
                Email = emailTrim,
                StudentId = studentIdUpper,
                PasswordHash = finalHash, // Lưu mật khẩu đúng ý Admin nhập
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            // Phân quyền cho tài khoản mới tạo
            if (!string.IsNullOrEmpty(model.RoleName))
            {
                // Tìm vai trò khớp (Ví dụ Admin gửi lên tiếng Việt hoặc tiếng Anh thì so khớp logic của bạn)
                // Lưu ý: Nếu trong DB lưu 'Student' mà form gửi lên 'Sinh viên', bạn có thể cần map lại đoạn này.
                var selectedRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName.ToLower() == model.RoleName.ToLower());
                if (selectedRole != null)
                {
                    var userRole = new UserRoles
                    {
                        UserId = newUser.UserId,
                        RoleId = selectedRole.RoleId,
                        AssignedAt = DateTime.UtcNow
                    };
                    _context.UserRoles.Add(userRole);
                    await _context.SaveChangesAsync();
                }
            }

            return Json(new { success = true, message = $"Thêm mới tài khoản thành công! Mật khẩu là: {actualPassword}" });
        }
        catch (Exception ex)
        {
            var innerMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            return Json(new { success = false, message = "Lỗi Database: " + innerMsg });
        }
    }

    // FIX LỖI 404 KHI XÓA TÀI KHOẢN + chặn xóa chính mình / xóa Admin + dọn dữ liệu liên quan
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser([FromForm] int id)
    {
        if (id <= 0)
        {
            return Json(new { success = false, message = "ID tài khoản không hợp lệ." });
        }

        try
        {
            var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (id == currentUserId)
                return Json(new { success = false, message = "Không thể xóa tài khoản của chính mình." });

            var user = await _context.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng này trên hệ thống." });
            }

            bool isAdmin = user.UserRoles.Any(ur => ur.Role != null && ur.Role.RoleName == "Admin");
            if (isAdmin)
                return Json(new { success = false, message = "Không thể xóa tài khoản Admin." });

            // Xóa sạch bảng phụ để tránh lỗi khóa ngoại
            var userRoles = _context.UserRoles.Where(ur => ur.UserId == id);
            _context.UserRoles.RemoveRange(userRoles);

            var teamMembers = _context.TeamMembers.Where(tm => tm.UserId == id);
            _context.TeamMembers.RemoveRange(teamMembers);

            var taskCompletions = _context.TaskCompletions.Where(tc => tc.CompletedBy == id);
            _context.TaskCompletions.RemoveRange(taskCompletions);

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã xóa người dùng thành công." });
        }
        catch (Exception ex)
        {
            var inner = ex.InnerException?.Message ?? ex.Message;
            return Json(new { success = false, message = $"Lỗi không thể xóa: {inner}" });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ToggleUserStatus(int id)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return Json(new { success = false, message = "Không tìm thấy tài khoản cần xử lý." });

            user.IsActive = !user.IsActive;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            string msg = user.IsActive ? "Đã mở khóa tài khoản thành công!" : "Đã khóa tài khoản thành công!";
            return Json(new { success = true, message = msg, isActive = user.IsActive });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Thao tác thất bại: " + ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateTestCompetition(string name, string status, DateTime startDate, DateTime endDate)
    {
        try
        {
            if (string.IsNullOrEmpty(name)) return Json(new { success = false, message = "Tên cuộc thi không được để trống!" });

            var newComp = new Competitions
            {
                CompetitionName = name.Trim(),
                Description = "Cuộc thi thử nghiệm hệ thống",
                Status = string.IsNullOrEmpty(status) ? "Đang hoạt động" : status,
                StartDate = startDate == default ? DateTime.UtcNow : startDate,
                EndDate = endDate == default ? DateTime.UtcNow.AddDays(30) : endDate,
                CreatedAt = DateTime.UtcNow
            };

            _context.Competitions.Add(newComp);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Tạo cuộc thi thành công! Thống kê sẽ được cập nhật." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lỗi khi tạo cuộc thi: " + ex.Message });
        }
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetReportData(string schoolYear, string department, string contestType, DateTime? fromDate, DateTime? toDate)
    {
        try
        {
            var totalUsers = await _context.Users.CountAsync();
            var totalStudents = await _context.UserRoles.CountAsync(ur => ur.Role.RoleName == "Student" || ur.Role.RoleName == "Sinh viên");
            var totalLecturers = await _context.UserRoles.CountAsync(ur => ur.Role.RoleName == "Lecturer" || ur.Role.RoleName == "Giảng viên");
            var totalJudges = await _context.UserRoles.CountAsync(ur => ur.Role.RoleName == "Judge" || ur.Role.RoleName == "Ban giám khảo");
            var totalOrganizers = await _context.UserRoles.CountAsync(ur => ur.Role.RoleName == "Organizer" || ur.Role.RoleName == "Ban tổ chức");
            var totalLocked = await _context.Users.CountAsync(u => u.IsActive == false);

            var totalCompetitions = await _context.Competitions.CountAsync();
            var totalSubmissions = await _context.Submissions.CountAsync();
            var totalRegistrations = await _context.Registrations.CountAsync();

            double completionRate = 0;
            if (totalRegistrations > 0)
                completionRate = Math.Round(((double)totalSubmissions / totalRegistrations) * 100, 1);

            var lineLabels = new string[6];
            var lineRegistrations = new int[6];
            var lineSubmissions = new int[6];

            for (int i = 0; i < 6; i++)
            {
                var targetDate = DateTime.UtcNow.AddMonths(-(5 - i));
                lineLabels[i] = $"Tháng {targetDate.Month}";
                lineRegistrations[i] = await _context.Registrations
                    .CountAsync(r => r.RegistrationDate.Month == targetDate.Month && r.RegistrationDate.Year == targetDate.Year);
                lineSubmissions[i] = await _context.Submissions
                    .CountAsync(s => s.SubmissionDate.Month == targetDate.Month && s.SubmissionDate.Year == targetDate.Year);
            }

            var approvedCount = await _context.Submissions.CountAsync(s => s.Status == "Đã duyệt" || s.Status == "Approved");
            var rejectedCount = await _context.Submissions.CountAsync(s => s.Status == "Từ chối" || s.Status == "Rejected");
            var pendingCount = totalSubmissions - (approvedCount + rejectedCount);

            return Json(new
            {
                success = true,
                cards = new
                {
                    totalCompetitions,
                    totalStudents,
                    totalSubmissions,
                    completionRate
                },
                accounts = new
                {
                    total = totalUsers,
                    students = totalStudents,
                    teachers = totalLecturers,
                    organizers = totalOrganizers,
                    judges = totalJudges,
                    locked = totalLocked
                },
                lineChart = new
                {
                    labels = lineLabels,
                    registrations = lineRegistrations,
                    submissions = lineSubmissions
                },
                doughnutChart = new
                {
                    labels = new[] { "Đã duyệt", "Chờ xử lý", "Từ chối" },
                    data = new[] { approvedCount, pendingCount, rejectedCount }
                },
                facultyChart = new
                {
                    labels = new[] { "Khoa CNTT", "Khoa Kinh tế", "Phòng QLKH", "Khoa Ngoại ngữ" },
                    data = new[] { totalSubmissions, 0, 0, 0 }
                }
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lỗi: " + ex.Message });
        }
    }

    [Authorize(Roles = "Admin")]
    public IActionResult SystemSettings() { return View(); }

    [Authorize(Roles = "Admin")]
    public IActionResult Reports() { return View(); }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SystemNotifications(string? search)
    {
        var (total, items) = await _notificationsService.GetPublicAnnouncementsPagedAsync(search, 1, 50);

        ViewData["TotalBroadcasts"] = total.ToString("N0");
        ViewData["Search"] = search;
        // Cuộc thi để gán vào "Cuộc thi liên quan" (tuỳ chọn)
        ViewData["Competitions"] = await _context.Competitions
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new { c.CompetitionId, c.CompetitionName })
            .ToListAsync();

        return View(items);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [RequestSizeLimit(20 * 1024 * 1024)] // 20 MB
    public async Task<IActionResult> CreateSystemAnnouncement(
        [FromForm] string title,
        [FromForm] string content,
        [FromForm] string type,
        [FromForm] int? relatedCompetitionId,
        IFormFile? imageFile,
        IFormFile? attachmentFile)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
                return Json(new { ok = false, message = "Tiêu đề và nội dung không được để trống." });

            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "announcements");
            Directory.CreateDirectory(uploadsDir);

            string? imageUrl = null;
            if (imageFile != null && imageFile.Length > 0)
            {
                var ext = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                var allowedImg = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                if (!allowedImg.Contains(ext))
                    return Json(new { ok = false, message = "Chỉ chấp nhận ảnh JPG, PNG, GIF, WEBP." });

                var imgName = $"img_{Guid.NewGuid():N}{ext}";
                var imgPath = Path.Combine(uploadsDir, imgName);
                await using var fs = System.IO.File.Create(imgPath);
                await imageFile.CopyToAsync(fs);
                imageUrl = $"/uploads/announcements/{imgName}";
            }

            string? attachUrl = null;
            string? attachName = null;
            if (attachmentFile != null && attachmentFile.Length > 0)
            {
                var ext = Path.GetExtension(attachmentFile.FileName).ToLowerInvariant();
                var allowedFile = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx" };
                if (!allowedFile.Contains(ext))
                    return Json(new { ok = false, message = "Chỉ chấp nhận file PDF, Word, Excel." });

                var fileName = $"attach_{Guid.NewGuid():N}{ext}";
                var filePath = Path.Combine(uploadsDir, fileName);
                await using var fs = System.IO.File.Create(filePath);
                await attachmentFile.CopyToAsync(fs);
                attachUrl  = $"/uploads/announcements/{fileName}";
                attachName = attachmentFile.FileName;
            }

            var currentUserId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : (int?)null;

            var announcement = new PublicAnnouncements
            {
                Title                = title.Trim(),
                Content              = content.Trim(),
                Type                 = string.IsNullOrEmpty(type) ? "Info" : type,
                ImageUrl             = imageUrl,
                AttachmentUrl        = attachUrl,
                AttachmentFileName   = attachName,
                RelatedCompetitionId = relatedCompetitionId,
                CreatedByUserId      = currentUserId,
                CreatedAt            = DateTime.UtcNow,
                IsPublished          = true
            };

            var id = await _notificationsService.CreateAnnouncementAsync(announcement);
            return Json(new { ok = true, message = "Đã đăng thông báo thành công.", id });
        }
        catch (Exception ex)
        {
            return Json(new { ok = false, message = "Lỗi hệ thống khi tạo thông báo: " + ex.Message });
        }
    }

    [Authorize(Roles = "Admin")]
    public IActionResult ActivityLogs() { return View(); }

    // ==========================================
    // NHẬT KÝ HOẠT ĐỘNG (TỔNG HỢP TỪ DỮ LIỆU THỰC CÓ TIMESTAMP)
    // Hệ thống chưa có bảng AuditLog riêng nên các mốc thời gian sẵn có
    // trên Users, UserRoles, Competitions, Registrations,
    // RegistrationEditHistory, Submissions, TaskCompletions,
    // PublicAnnouncements được gộp lại thành một dòng thời gian duy nhất.
    // ==========================================

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetActivityLogs(string search, string module, [FromQuery(Name = "action")] string actionType, DateTime? date, int page = 1, int pageSize = 15)
    {
        try
        {
            var events = new List<ActivityLogEntry>();

            var users = await _context.Users
                .Select(u => new { u.UserId, u.FullName, u.Email, u.IsActive, u.CreatedAt, u.UpdatedAt, u.LastLogin })
                .ToListAsync();
            var userDict = users.ToDictionary(u => u.UserId, u => u);

            var roleAssignments = await _context.UserRoles
                .Select(ur => new { ur.UserId, ur.AssignedAt, RoleName = ur.Role.RoleName })
                .ToListAsync();
            var roleLookup = roleAssignments
                .GroupBy(x => x.UserId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.AssignedAt).First().RoleName);

            string RoleOf(int userId) => roleLookup.TryGetValue(userId, out var r) ? r : "Chưa phân quyền";

            // 1. Users: tạo tài khoản / đăng nhập gần nhất / cập nhật hồ sơ hoặc khóa-mở khóa
            foreach (var u in users)
            {
                events.Add(new ActivityLogEntry
                {
                    Timestamp = u.CreatedAt,
                    UserName = u.FullName,
                    UserEmail = u.Email,
                    RoleName = RoleOf(u.UserId),
                    ActionType = "CREATE",
                    Module = "Tài khoản",
                    Description = "Tài khoản được tạo mới trên hệ thống"
                });

                if (u.LastLogin.HasValue)
                {
                    events.Add(new ActivityLogEntry
                    {
                        Timestamp = u.LastLogin.Value,
                        UserName = u.FullName,
                        UserEmail = u.Email,
                        RoleName = RoleOf(u.UserId),
                        ActionType = "LOGIN",
                        Module = "Đăng nhập",
                        Description = "Đăng nhập vào hệ thống"
                    });
                }

                if (u.UpdatedAt.HasValue)
                {
                    events.Add(new ActivityLogEntry
                    {
                        Timestamp = u.UpdatedAt.Value,
                        UserName = u.FullName,
                        UserEmail = u.Email,
                        RoleName = RoleOf(u.UserId),
                        ActionType = "UPDATE",
                        Module = "Tài khoản",
                        Description = u.IsActive
                            ? "Thông tin tài khoản được cập nhật (hồ sơ hoặc mở khóa)"
                            : "Thông tin tài khoản được cập nhật (hồ sơ hoặc bị khóa)"
                    });
                }
            }

            // 2. Phân quyền
            foreach (var ra in roleAssignments)
            {
                var u = userDict.TryGetValue(ra.UserId, out var uu) ? uu : null;
                events.Add(new ActivityLogEntry
                {
                    Timestamp = ra.AssignedAt,
                    UserName = u?.FullName ?? "Không rõ",
                    UserEmail = u?.Email ?? "",
                    RoleName = ra.RoleName,
                    ActionType = "UPDATE",
                    Module = "Phân quyền",
                    Description = $"Được gán quyền '{ra.RoleName}'"
                });
            }

            // 3. Cuộc thi
            var competitions = await _context.Competitions
                .Select(c => new { c.CompetitionName, c.CreatedAt, c.UpdatedAt, c.CreatedByUserId })
                .ToListAsync();
            foreach (var c in competitions)
            {
                string creatorName = "Hệ thống", creatorEmail = "", creatorRole = "";
                if (c.CreatedByUserId.HasValue && userDict.TryGetValue(c.CreatedByUserId.Value, out var creator))
                {
                    creatorName = creator.FullName;
                    creatorEmail = creator.Email;
                    creatorRole = RoleOf(creator.UserId);
                }

                events.Add(new ActivityLogEntry
                {
                    Timestamp = c.CreatedAt,
                    UserName = creatorName,
                    UserEmail = creatorEmail,
                    RoleName = creatorRole,
                    ActionType = "CREATE",
                    Module = "Cuộc thi",
                    Description = $"Tạo mới cuộc thi '{c.CompetitionName}'"
                });

                if (c.UpdatedAt.HasValue)
                {
                    events.Add(new ActivityLogEntry
                    {
                        Timestamp = c.UpdatedAt.Value,
                        UserName = creatorName,
                        UserEmail = creatorEmail,
                        RoleName = creatorRole,
                        ActionType = "UPDATE",
                        Module = "Cuộc thi",
                        Description = $"Cập nhật thông tin cuộc thi '{c.CompetitionName}'"
                    });
                }
            }

            // 4. Đăng ký
            var registrations = await _context.Registrations
                .Select(r => new { r.UserId, r.RegistrationDate, r.ApprovalDate, r.Status, CompetitionName = r.Competition.CompetitionName })
                .ToListAsync();
            foreach (var r in registrations)
            {
                var u = userDict.TryGetValue(r.UserId, out var uu) ? uu : null;
                events.Add(new ActivityLogEntry
                {
                    Timestamp = r.RegistrationDate,
                    UserName = u?.FullName ?? "Không rõ",
                    UserEmail = u?.Email ?? "",
                    RoleName = RoleOf(r.UserId),
                    ActionType = "CREATE",
                    Module = "Đăng ký",
                    Description = $"Đăng ký tham gia cuộc thi '{r.CompetitionName}'"
                });

                if (r.ApprovalDate.HasValue)
                {
                    events.Add(new ActivityLogEntry
                    {
                        Timestamp = r.ApprovalDate.Value,
                        UserName = u?.FullName ?? "Không rõ",
                        UserEmail = u?.Email ?? "",
                        RoleName = RoleOf(r.UserId),
                        ActionType = r.Status == "Rejected" ? "DELETE" : "UPDATE",
                        Module = "Đăng ký",
                        Description = $"Hồ sơ đăng ký '{r.CompetitionName}' được " +
                            (r.Status == "Approved" ? "duyệt" : r.Status == "Rejected" ? "từ chối" : "cập nhật")
                    });
                }
            }

            // 5. Lịch sử chỉnh sửa đăng ký (bảng audit thực sự đã có sẵn)
            var editHistory = await _context.RegistrationEditHistories
                .Select(h => new
                {
                    h.EditedAt,
                    h.ActionType,
                    h.ChangesSummary,
                    h.EditedBy,
                    EditorName = h.Editor.FullName,
                    EditorEmail = h.Editor.Email,
                    CompetitionName = h.Registration.Competition.CompetitionName
                })
                .ToListAsync();
            foreach (var h in editHistory)
            {
                string mappedAction = h.ActionType switch
                {
                    "OrganizerRejected" => "DELETE",
                    _ => "UPDATE"
                };
                events.Add(new ActivityLogEntry
                {
                    Timestamp = h.EditedAt,
                    UserName = h.EditorName,
                    UserEmail = h.EditorEmail,
                    RoleName = RoleOf(h.EditedBy),
                    ActionType = mappedAction,
                    Module = "Đăng ký",
                    Description = h.ChangesSummary ?? $"Chỉnh sửa hồ sơ đăng ký '{h.CompetitionName}'"
                });
            }

            // 6. Bài dự thi
            var submissions = await _context.Submissions
                .Select(s => new
                {
                    s.Title,
                    s.SubmissionDate,
                    s.UpdatedAt,
                    CompetitionName = s.Competition.CompetitionName,
                    SubmitterUserId = s.Registration != null ? (int?)s.Registration.UserId : null,
                    SubmitterName = s.Registration != null ? s.Registration.User.FullName : (s.Team != null ? s.Team.TeamName : "Không rõ"),
                    SubmitterEmail = s.Registration != null ? s.Registration.User.Email : ""
                })
                .ToListAsync();
            foreach (var s in submissions)
            {
                var role = s.SubmitterUserId.HasValue ? RoleOf(s.SubmitterUserId.Value) : "";
                events.Add(new ActivityLogEntry
                {
                    Timestamp = s.SubmissionDate,
                    UserName = s.SubmitterName,
                    UserEmail = s.SubmitterEmail,
                    RoleName = role,
                    ActionType = "CREATE",
                    Module = "Bài dự thi",
                    Description = $"Nộp bài dự thi '{s.Title}' cho cuộc thi '{s.CompetitionName}'"
                });

                if (s.UpdatedAt.HasValue)
                {
                    events.Add(new ActivityLogEntry
                    {
                        Timestamp = s.UpdatedAt.Value,
                        UserName = s.SubmitterName,
                        UserEmail = s.SubmitterEmail,
                        RoleName = role,
                        ActionType = "UPDATE",
                        Module = "Bài dự thi",
                        Description = $"Cập nhật bài dự thi '{s.Title}'"
                    });
                }
            }

            // 7. Nhiệm vụ nhóm
            var completions = await _context.TaskCompletions
                .Select(tc => new
                {
                    tc.CompletedAt,
                    tc.IsVerified,
                    tc.VerifiedAt,
                    tc.CompletedBy,
                    tc.VerifiedBy,
                    CompletedByName = tc.CompletedByUser.FullName,
                    CompletedByEmail = tc.CompletedByUser.Email,
                    TaskTitle = tc.Task.Title,
                    VerifiedByName = tc.VerifiedByUser != null ? tc.VerifiedByUser.FullName : null,
                    VerifiedByEmail = tc.VerifiedByUser != null ? tc.VerifiedByUser.Email : null
                })
                .ToListAsync();
            foreach (var tc in completions)
            {
                events.Add(new ActivityLogEntry
                {
                    Timestamp = tc.CompletedAt,
                    UserName = tc.CompletedByName,
                    UserEmail = tc.CompletedByEmail,
                    RoleName = RoleOf(tc.CompletedBy),
                    ActionType = "UPDATE",
                    Module = "Nhiệm vụ nhóm",
                    Description = $"Đánh dấu hoàn thành nhiệm vụ '{tc.TaskTitle}'"
                });

                if (tc.IsVerified && tc.VerifiedAt.HasValue && tc.VerifiedBy.HasValue)
                {
                    events.Add(new ActivityLogEntry
                    {
                        Timestamp = tc.VerifiedAt.Value,
                        UserName = tc.VerifiedByName ?? "Giảng viên",
                        UserEmail = tc.VerifiedByEmail ?? "",
                        RoleName = RoleOf(tc.VerifiedBy.Value),
                        ActionType = "UPDATE",
                        Module = "Nhiệm vụ nhóm",
                        Description = $"Xác nhận hoàn thành nhiệm vụ '{tc.TaskTitle}'"
                    });
                }
            }

            // 8. Thông báo công khai
            var announcements = await _context.PublicAnnouncements
                .Select(a => new { a.Title, a.CreatedAt, a.CreatedByUserId })
                .ToListAsync();
            foreach (var a in announcements)
            {
                string name = "Hệ thống", email = "", role = "";
                if (a.CreatedByUserId.HasValue && userDict.TryGetValue(a.CreatedByUserId.Value, out var creator))
                {
                    name = creator.FullName;
                    email = creator.Email;
                    role = RoleOf(creator.UserId);
                }
                events.Add(new ActivityLogEntry
                {
                    Timestamp = a.CreatedAt,
                    UserName = name,
                    UserEmail = email,
                    RoleName = role,
                    ActionType = "CREATE",
                    Module = "Thông báo",
                    Description = $"Đăng thông báo '{a.Title}'"
                });
            }

            // --- Thống kê tổng quan (tính trên toàn bộ dữ liệu, không áp bộ lọc) ---
            var now = DateTime.UtcNow;
            var todayLocal = now.ToLocalTime().Date;
            var stats = new
            {
                Last24h = events.Count(e => e.Timestamp >= now.AddHours(-24)),
                LoginsToday = users.Count(u => u.LastLogin.HasValue && u.LastLogin.Value.ToLocalTime().Date == todayLocal),
                PendingRegistrations = await _context.Registrations.CountAsync(r => r.Status == "Pending"),
                LockedAccounts = users.Count(u => u.IsActive == false)
            };

            // --- Hoạt động quan trọng gần đây (xóa / phân quyền) cho panel bên phải ---
            var importantList = events
                .Where(e => e.ActionType == "DELETE" || e.Module == "Phân quyền")
                .OrderByDescending(e => e.Timestamp)
                .Take(5)
                .Select(e => new
                {
                    e.UserName,
                    e.Description,
                    e.ActionType,
                    TimeAgo = FormatTimeAgo(now - e.Timestamp)
                })
                .ToList();

            // --- Áp bộ lọc & phân trang ---
            IEnumerable<ActivityLogEntry> filtered = events;

            if (!string.IsNullOrEmpty(search))
            {
                var s = search.ToLower().Trim();
                filtered = filtered.Where(e => (e.UserName ?? "").ToLower().Contains(s) || (e.UserEmail ?? "").ToLower().Contains(s));
            }
            if (!string.IsNullOrEmpty(module))
                filtered = filtered.Where(e => e.Module == module);
            if (!string.IsNullOrEmpty(actionType))
                filtered = filtered.Where(e => e.ActionType == actionType);
            if (date.HasValue)
                filtered = filtered.Where(e => e.Timestamp.ToLocalTime().Date == date.Value.Date);

            var ordered = filtered.OrderByDescending(e => e.Timestamp).ToList();
            var totalRecords = ordered.Count;
            var totalPages = totalRecords == 0 ? 1 : (int)Math.Ceiling(totalRecords / (double)pageSize);
            page = Math.Max(1, Math.Min(page, totalPages));

            var pagedItems = ordered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new
                {
                    Time = e.Timestamp.ToLocalTime().ToString("HH:mm:ss"),
                    DateText = e.Timestamp.ToLocalTime().ToString("dd/MM/yyyy"),
                    e.UserName,
                    e.UserEmail,
                    e.RoleName,
                    e.ActionType,
                    e.Module,
                    e.Description
                })
                .ToList();

            return Json(new
            {
                success = true,
                stats,
                important = importantList,
                totalRecords,
                totalPages,
                page,
                items = pagedItems
            });
        }
        catch (Exception ex)
        {
            var inner = ex.InnerException?.Message ?? ex.Message;
            return Json(new { success = false, message = "Lỗi tải nhật ký hoạt động: " + inner });
        }
    }

    private static string FormatTimeAgo(TimeSpan span)
    {
        if (span.TotalMinutes < 1) return "Vừa xong";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} phút trước";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours} giờ trước";
        return $"{(int)span.TotalDays} ngày trước";
    }
}