using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web_cham_diem.Models;
using Web_cham_diem.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Web_cham_diem.Services; // Đảm bảo nạp đúng Namespace chứa IAuditLogService

namespace Web_cham_diem.Controllers;

[Authorize]
public class AccountController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AccountController(
        ApplicationDbContext context,
        IAuditLogService auditLogService,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _auditLogService = auditLogService;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Hàm bổ trợ lấy IP chính xác của Client (kể cả sau Proxy/Load Balancer)
    /// </summary>
    private string GetClientIp()
    {
        var ip = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
        if (_httpContextAccessor.HttpContext?.Request.Headers.ContainsKey("X-Forwarded-For") == true)
        {
            ip = _httpContextAccessor.HttpContext.Request.Headers["X-Forwarded-For"].ToString().Split(',')[0].Trim();
        }
        return ip ?? "unknown";
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
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    // ==================== LOGIN ====================
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        string ip = GetClientIp();
        var emailTrim = (request.Email ?? "").ToLower().Trim();

        if (string.IsNullOrEmpty(emailTrim) || string.IsNullOrEmpty(request.Password))
        {
            await _auditLogService.LogAsync(null, emailTrim, "-", "LOGIN", "Auth", null, "Thiếu thông tin", ip, false);
            return Json(new { success = false, message = "Vui lòng nhập đầy đủ Email và Mật khẩu." });
        }

        try
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == emailTrim);

            if (user == null)
            {
                await _auditLogService.LogAsync(null, emailTrim, "-", "LOGIN", "Auth", null, "Email không tồn tại", ip, false);
                return Json(new { success = false, message = "Tài khoản không tồn tại." });
            }

            if (!user.IsActive)
            {
                await _auditLogService.LogAsync(user.UserId, user.Email, "Locked", "LOGIN", "Auth", null, "Tài khoản bị khóa", ip, false);
                return Json(new { success = false, message = "Tài khoản bị khóa." });
            }

            bool isValidPassword = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

            if (!isValidPassword)
            {
                await _auditLogService.LogAsync(user.UserId, user.Email, "-", "LOGIN", "Auth", null, "Sai mật khẩu", ip, false);
                return Json(new { success = false, message = "Mật khẩu không chính xác." });
            }

            var roleName = user.UserRoles?.FirstOrDefault()?.Role?.RoleName ?? "Student";

            // Đăng nhập
            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.FullName ?? ""),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, roleName)
        };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
                new AuthenticationProperties { IsPersistent = true });

            // GHI LOG LOGIN - quan trọng
            await _auditLogService.LogAsync(user.UserId, user.Email, roleName, "LOGIN", "Auth", null, "Đăng nhập thành công", ip, true);

            user.LastLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đăng nhập thành công!", role = roleName });
        }
        catch (Exception ex)
        {
            await _auditLogService.LogAsync(null, emailTrim, "-", "LOGIN", "Auth", null, ex.Message, ip, false);
            return Json(new { success = false, message = "Lỗi hệ thống." });
        }
    }

    // ==================== LOGOUT ====================
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int? userId = int.TryParse(userIdClaim, out int uid) ? uid : null;
            string email = User.FindFirst(ClaimTypes.Email)?.Value ?? "unknown";
            string role = User.FindFirst(ClaimTypes.Role)?.Value ?? "-";

            await _auditLogService.LogAsync(userId, email, role, "LOGOUT", "Auth", null, "Đăng xuất thành công", GetClientIp(), true);
        }
        catch { }

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
            await _context.SaveChangesAsync();

            // Ghi nhận nhật ký thay đổi thông tin người dùng
            int currentAdminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            await _auditLogService.LogAsync(currentAdminId, User.FindFirst(ClaimTypes.Email)?.Value, User.FindFirst(ClaimTypes.Role)?.Value,
                "UPDATE", "UserManagement", userId, $"Cập nhật thông tin cơ bản của người dùng ID {userId}", GetClientIp(), true);

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

            // Ghi nhận nhật ký thay đổi phân quyền
            int currentAdminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            await _auditLogService.LogAsync(currentAdminId, User.FindFirst(ClaimTypes.Email)?.Value, User.FindFirst(ClaimTypes.Role)?.Value,
                "UPDATE", "UserManagement", userId, $"Thay đổi phân quyền người dùng ID {userId} sang nhóm '{roleName}'", GetClientIp(), true);

            return Json(new { success = true, message = "Đã cập nhật phân quyền mới thành công!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lỗi hệ thống phân quyền: " + ex.Message });
        }
    }

    public class CreateUserRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class AdminCreateUserForm
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? StudentId { get; set; }
        public string? RoleName { get; set; }
        public string? Password { get; set; }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateUser([FromForm] AdminCreateUserForm model)
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

            // Lấy mật khẩu chỉ định từ Admin hoặc dùng mật khẩu mặc định
            var actualPassword = string.IsNullOrEmpty(model.Password) ? "UEF@12345" : model.Password;
            string finalHash = BCrypt.Net.BCrypt.HashPassword(actualPassword);

            var newUser = new Users
            {
                FullName = model.FullName.Trim(),
                Email = emailTrim,
                StudentId = studentIdUpper,
                PasswordHash = finalHash,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            // Phân quyền cho tài khoản mới tạo
            if (!string.IsNullOrEmpty(model.RoleName))
            {
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

            // Ghi nhật ký hệ thống hành vi Thêm mới người dùng
            int currentAdminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            await _auditLogService.LogAsync(currentAdminId, User.FindFirst(ClaimTypes.Email)?.Value, User.FindFirst(ClaimTypes.Role)?.Value,
                "CREATE", "UserManagement", newUser.UserId, $"Tạo mới người dùng: {newUser.Email} - Nhóm: {model.RoleName}", GetClientIp(), true);

            return Json(new { success = true, message = $"Thêm mới tài khoản thành công! Mật khẩu là: {actualPassword}" });
        }
        catch (Exception ex)
        {
            var innerMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            return Json(new { success = false, message = "Lỗi Database: " + innerMsg });
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser([FromForm] int id)
    {
        if (id <= 0)
        {
            return Json(new { success = false, message = "ID tài khoản không hợp lệ." });
        }

        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng này trên hệ thống." });
            }

            // Xóa sạch các bảng phụ phụ thuộc để tránh lỗi khóa ngoại (Foreign Key)
            var userRoles = _context.UserRoles.Where(ur => ur.UserId == id);
            _context.UserRoles.RemoveRange(userRoles);

            var teamMembers = _context.TeamMembers.Where(tm => tm.UserId == id);
            _context.TeamMembers.RemoveRange(teamMembers);

            var taskCompletions = _context.TaskCompletions.Where(tc => tc.CompletedBy == id);
            _context.TaskCompletions.RemoveRange(taskCompletions);

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            // Ghi nhật ký hành vi Xóa người dùng
            int currentAdminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            await _auditLogService.LogAsync(currentAdminId, User.FindFirst(ClaimTypes.Email)?.Value, User.FindFirst(ClaimTypes.Role)?.Value,
                "DELETE", "UserManagement", id, $"Đã xóa người dùng hệ thống: {user.Email} (ID: {id})", GetClientIp(), true);

            return Json(new { success = true, message = "Đã xóa người dùng thành công." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Lỗi không thể xóa: {ex.Message}" });
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
            await _context.SaveChangesAsync();

            string msg = user.IsActive ? "Đã mở khóa tài khoản thành công!" : "Đã khóa tài khoản thành công!";

            // Ghi nhật ký đổi trạng thái kích hoạt tài khoản
            int currentAdminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            await _auditLogService.LogAsync(currentAdminId, User.FindFirst(ClaimTypes.Email)?.Value, User.FindFirst(ClaimTypes.Role)?.Value,
                "UPDATE", "UserManagement", id, $"{msg} đối với người dùng ID {id}", GetClientIp(), true);

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

    // ==========================================
    // CÁC KHÔNG GIAN BỔ TRỢ KHÁC CỦA ADMIN
    // ==========================================

    [Authorize(Roles = "Admin")]
    public IActionResult SystemSettings() { return View(); }

    [Authorize(Roles = "Admin")]
    public IActionResult Reports() { return View(); }

    [Authorize(Roles = "Admin")]
    public IActionResult SystemNotifications() { return View(); }

    /// <summary>
    /// Trang hiển thị danh sách Nhật ký hoạt động của hệ thống dành cho Admin
    /// </summary>
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ActivityLogs(string? search, string? module, string? actionType, DateTime? date, int page = 1)
    {
        // Test log khi vào trang
        try
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            string email = User.FindFirst(ClaimTypes.Email)?.Value ?? "admin@test.com";
            string role = User.FindFirst(ClaimTypes.Role)?.Value ?? "Admin";

            await _auditLogService.LogAsync(userId, email, role, "VIEW", "ActivityLogs", null,
                "Truy cập trang Nhật ký", GetClientIp(), true);
        }
        catch (Exception ex)
        {
            Console.WriteLine("TEST ERROR: " + ex.Message);
        }

        var filter = new AuditLogFilter
        {
            Search = search,
            Module = module,
            ActionType = actionType,
            Date = date,
            Page = page,
            PageSize = 15
        };

        var viewModel = await _auditLogService.GetActivityLogsAsync(filter);
        return View(viewModel);
    }
}