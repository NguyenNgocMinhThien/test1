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

    // Đường dẫn truy cập: /Account/UserDashboard
    [Authorize(Roles = "Student,Admin")]
    public IActionResult UserDashboard()
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
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAdminMainDashboardData()
    {
        try
        {
            // 1. Thống kê tổng số lượng người dùng hệ thống
            var totalUsers = await _context.Users.CountAsync();

            // 2. Thống kê số lượng theo vai trò (Role) bằng cách đếm từ bảng liên kết UserRoles
            var totalStudents = await _context.UserRoles.CountAsync(ur => ur.Role.RoleName == "Student");
            var totalLecturers = await _context.UserRoles.CountAsync(ur => ur.Role.RoleName == "Lecturer" || ur.Role.RoleName == "Giảng viên");
            var totalJudges = await _context.UserRoles.CountAsync(ur => ur.Role.RoleName == "Judge" || ur.Role.RoleName == "Ban giám khảo");
            var totalAdmins = await _context.UserRoles.CountAsync(ur => ur.Role.RoleName == "Admin");

            // 3. Thống kê số lượng cuộc thi (Competitions)
            var totalContests = await _context.Competitions.CountAsync();
            var activeContests = await _context.Competitions.CountAsync(c => c.Status == "Đang hoạt động" || c.Status == "Active");

            // 4. Thống kê tổng số lượng bài làm đã nộp (Submissions)
            var totalSubmissions = await _context.Submissions.CountAsync();

            // 5. Chuẩn bị dữ liệu cho biểu đồ Cột (Số cuộc thi trong 5 năm gần đây)
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

    // KHÔI PHỤC: Tuyến đường hiển thị giao diện Quản lý tài khoản bị thiếu
    // Đường dẫn: /Account/UserManagement
    [Authorize(Roles = "Admin")]
    public IActionResult UserManagement()
    {
        return View();
    }

    // Tuyến đường hiển thị giao diện: /Account/ContestOverview
    [Authorize(Roles = "Admin")]
    public IActionResult ContestOverview()
    {
        return View();
    }

    // API TRUY VẤN DATABASE THẬT: Lấy số liệu thống kê động cho Admin Dashboard
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetRealContestOverviewData(string search, string status, string field, string schoolYear)
    {
        try
        {
            // 1. Khởi tạo Query gốc truy vấn từ bảng Cuộc thi thực tế (Competitions) trong DbContext
            var competitionQuery = _context.Competitions.AsQueryable();

            // 2. Thực hiện lọc dữ liệu theo bộ gõ tìm kiếm và Dropdown từ UI gửi lên
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

            // Lấy danh sách cuộc thi thực tế sau khi đã lọc dữ liệu để hiển thị cho phần Monitoring
            var rawCompetitions = await competitionQuery.OrderByDescending(c => c.CreatedAt).ToListAsync();

            // 3. Đếm dữ liệu thực tế phục vụ 6 thẻ trạng thái
            var totalCompetitions = await _context.Competitions.CountAsync();
            var activeCompetitions = await _context.Competitions.CountAsync(c => c.Status == "Đang hoạt động" || c.Status == "Active");
            var upcomingCompetitions = await _context.Competitions.CountAsync(c => c.Status == "Sắp diễn ra" || c.Status == "Upcoming");
            var completedCompetitions = await _context.Competitions.CountAsync(c => c.Status == "Đã kết thúc" || c.Status == "Completed");

            // Đếm tổng số lượt thí sinh đăng ký dự thi (Registrations) và tổng số bài đã nộp từ DB (Submissions)
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

            // 4. Biểu đồ đường tròn (Doughnut): Đếm số lượng cuộc thi phân bổ theo Trạng thái
            var statusGroup = await _context.Competitions
                .GroupBy(c => c.Status)
                .Select(g => new { StatusName = g.Key ?? "Chưa rõ", Count = g.Count() })
                .ToListAsync();

            var fieldData = new
            {
                Labels = statusGroup.Select(f => f.StatusName).ToArray(),
                Data = statusGroup.Select(f => f.Count).ToArray()
            };

            // 5. Biểu đồ hỗn hợp (6 Tháng gần nhất): Tính số cuộc thi mới tạo và lượng bài nộp từ thí sinh từng tháng
            var monthLabels = new string[6];
            var newContestCounts = new int[6];
            var submissionCounts = new int[6];

            for (int i = 0; i < 6; i++)
            {
                var targetDate = DateTime.UtcNow.AddMonths(-i);
                monthLabels[5 - i] = $"Tháng {targetDate.Month}";

                // Đếm số lượng thực tế trong Database theo từng tháng cụ thể thông qua trường CreatedAt
                newContestCounts[5 - i] = await _context.Competitions
                    .CountAsync(c => c.CreatedAt.Month == targetDate.Month && c.CreatedAt.Year == targetDate.Year);

                // ĐÃ SỬA: Sử dụng trường cấu trúc thật s.SubmissionDate từ cơ sở dữ liệu của bạn
                // Ví dụ nếu tên cột chính xác của bạn trong DB là SubmissionDate:
                submissionCounts[5 - i] = await _context.Submissions
                    .CountAsync(s => s.SubmissionDate.Month == targetDate.Month && s.SubmissionDate.Year == targetDate.Year);
            }

            var monthlyData = new
            {
                Labels = monthLabels,
                NewContests = newContestCounts,
                Submissions = submissionCounts
            };

            // 6. Xử lý dữ liệu hiển thị bảng Monitoring (Giám sát % tiến độ dựa trên mốc thời gian thật)
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
                    // Kiểm tra nếu ngày kết thúc khác ngày mặc định tối thiểu của hệ thống
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

        var result = userList.OrderByDescending(u => u.UserId).ToList();
        return Json(result);
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

        if (user == null)
        {
            return Json(new { success = false, message = "Không tìm thấy người dùng này." });
        }

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
            if (string.IsNullOrEmpty(name))
            {
                return Json(new { success = false, message = "Tên cuộc thi không được để trống!" });
            }

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

    // Các action bổ sung cho thanh Menu Hệ thống
    [Authorize(Roles = "Admin")]
    public IActionResult SystemSettings() { return View(); }

    [Authorize(Roles = "Admin")]
    public IActionResult Reports() { return View(); }

    [Authorize(Roles = "Admin")]
    public IActionResult SystemNotifications() { return View(); }

    [Authorize(Roles = "Admin")]
    public IActionResult ActivityLogs() { return View(); }
}