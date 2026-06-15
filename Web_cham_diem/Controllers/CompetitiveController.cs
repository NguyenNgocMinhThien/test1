using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Web_cham_diem.Models;
using Web_cham_diem.Models.ViewModels;
using Web_cham_diem.Services;

namespace Web_cham_diem.Controllers;

public class CompetitiveController : Controller
{
    private readonly ICompetitionService _competitionService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CompetitiveController> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly INotificationsService _notificationsService;

    public CompetitiveController(
        ICompetitionService competitionService,
        ApplicationDbContext context,
        ILogger<CompetitiveController> logger,
        IWebHostEnvironment environment,
        INotificationsService notificationsService)
    {
        _competitionService   = competitionService;
        _context              = context;
        _logger               = logger;
        _environment          = environment;
        _notificationsService = notificationsService;
    }

    // ───────── user notification API ─────────

    [HttpGet("/api/user/notifications")]
    public async Task<IActionResult> GetUserNotifications([FromQuery] int take = 10)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out int userId))
            return Json(new { unread = 0, items = Array.Empty<object>() });

        var unread = await _notificationsService.GetUnreadCountAsync(userId);
        var items  = await _notificationsService.GetRecentForUserAsync(userId, take);
        return Json(new { unread, items });
    }

    [HttpPost("/api/user/notifications/{id:int}/read")]
    public async Task<IActionResult> MarkNotificationRead(int id)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out int userId))
            return Json(new { ok = false });

        var ok = await _notificationsService.MarkReadAsync(id, userId);
        return Json(new { ok });
    }

    [HttpPost("/api/user/notifications/read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out int userId))
            return Json(new { ok = false, count = 0 });

        var count = await _notificationsService.MarkAllReadAsync(userId);
        return Json(new { ok = true, count });
    }

    // GET: /Notifications
    [HttpGet("/Notifications")]
    public async Task<IActionResult> UserNotifications(
        [FromQuery] string? type = null,
        [FromQuery] bool unreadOnly = false,
        [FromQuery] int page = 1)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out int userId))
        {
            // Unauthenticated — redirect to login
            return Redirect("/Login?returnUrl=/Notifications");
        }

        try
        {
            var vm = await _notificationsService.GetUserNotificationsPageAsync(userId, type, unreadOnly, page, pageSize: 15);
            return View("~/Views/Pages/Notifications.cshtml", vm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading user notifications page");
            return View("~/Views/Pages/Notifications.cshtml", new UserNotificationsPageViewModel());
        }
    }

    // GET: /api/competitions
    [HttpGet("/api/competitions")]
    public async Task<IActionResult> GetCompetitions()
    {
        try
        {
            var now = DateTime.UtcNow;
            var competitions = await _context.Competitions
                .Include(c => c.CompetitionImages)
                .Include(c => c.RegistrationRounds)
                .Where(c => c.Status != "Draft")
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            var result = competitions.Select(c => new
            {
                c.CompetitionId,
                c.CompetitionName,
                c.Description,
                c.Category,
                c.StartDate,
                c.EndDate,
                c.SubmissionDeadline,
                c.MaxParticipants,
                c.MaxTeamSize,
                c.Status,
                c.IsTeamBased,
                c.Prize,
                ThumbnailUrl = c.CompetitionImages.FirstOrDefault(i => i.IsThumbnail)?.ImageUrl
                               ?? c.CompetitionImages.FirstOrDefault()?.ImageUrl,
                LatestRegistrationDeadline = c.RegistrationRounds.Any()
                    ? (DateTime?)c.RegistrationRounds.Max(r => r.EndDate)
                    : null,
                HasActiveRound = c.RegistrationRounds.Any(r => now >= r.StartDate && now <= r.EndDate)
            }).ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Lỗi khi tải dữ liệu", message = ex.Message });
        }
    }

    // GET /api/users/lookup?studentId=xxx[&role=Lecturer]
    [Authorize]
    [HttpGet("/api/users/lookup")]
    public async Task<IActionResult> LookupUser([FromQuery] string studentId, [FromQuery] string role = "")
    {
        if (string.IsNullOrWhiteSpace(studentId))
            return BadRequest(new { message = "MSSV không được để trống." });

        var user = await _context.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.StudentId == studentId && u.IsActive);

        if (user == null)
            return NotFound(new { message = "Không tìm thấy người dùng với MSSV này." });

        if (!string.IsNullOrEmpty(role))
        {
            var hasRole = user.UserRoles.Any(ur => ur.Role.RoleName == role);
            if (!hasRole)
                return NotFound(new { message = $"Người dùng không có vai trò {role}." });
        }

        var userRole = user.UserRoles.FirstOrDefault()?.Role.RoleName ?? "Unknown";
        return Ok(new { user.UserId, user.FullName, user.Email, user.StudentId, role = userRole });
    }

    // GET: /Competitions/Register/{id}
    [Authorize(Roles = "Student,Lecturer")]
    [HttpGet]
    [Route("Competitions/Register/{id:int}")]
    public async Task<IActionResult> Register(int id)
    {
        var competition = await _context.Competitions
            .Include(c => c.RegistrationRounds)
            .FirstOrDefaultAsync(c => c.CompetitionId == id);
        if (competition == null)
            return NotFound("Cuộc thi không tồn tại.");

        var nowUtc = DateTime.UtcNow;
        var hasActiveRound = competition.RegistrationRounds
            .Any(r => nowUtc >= r.StartDate && nowUtc <= r.EndDate);

        if (competition.Status != "Active" || nowUtc >= competition.StartDate || !hasActiveRound)
        {
            TempData["ErrorMessage"] = "Cuộc thi đã đóng đăng ký hoặc không trong đợt đăng ký.";
            return RedirectToAction("Details", new { id });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Index", "Login");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == int.Parse(userId));
        if (user == null)
            return RedirectToAction("Index", "Login");

        var isLecturer = User.IsInRole("Lecturer");

        // Student: check if already registered
        if (!isLecturer)
        {
            var isRegistered = await _context.Registrations
                .AnyAsync(r => r.CompetitionId == id && r.UserId == user.UserId && r.Status != "Withdrawn");
            if (isRegistered)
                return RedirectToAction("RegistrationDetail", new { id });
        }

        var viewModel = BuildRegistrationViewModel(competition, user);
        viewModel.IsLecturerFlow = isLecturer;
        return View("~/Views/Pages/StudentCompetitionRegistration.cshtml", viewModel);
    }

    // POST: /Competitions/Register/{id}
    [Authorize(Roles = "Student,Lecturer")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Competitions/Register/{id:int}")]
    public async Task<IActionResult> Register(int id, CompetitionRegistrationViewModel model)
    {
        var competition = await _context.Competitions
            .Include(c => c.RegistrationRounds)
            .FirstOrDefaultAsync(c => c.CompetitionId == id);
        if (competition == null)
            return NotFound("Cuộc thi không tồn tại.");

        var nowUtc = DateTime.UtcNow;
        var hasActiveRound = competition.RegistrationRounds
            .Any(r => nowUtc >= r.StartDate && nowUtc <= r.EndDate);

        if (competition.Status != "Active" || nowUtc >= competition.StartDate || !hasActiveRound)
        {
            TempData["ErrorMessage"] = "Cuộc thi đã đóng đăng ký hoặc không trong đợt đăng ký.";
            return RedirectToAction("Details", new { id });
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId))
            return RedirectToAction("Index", "Login");

        var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == int.Parse(currentUserId));
        if (currentUser == null)
            return RedirectToAction("Index", "Login");

        var isLecturer = User.IsInRole("Lecturer");

        // ── Xác định người đăng ký chính (thí sinh) và advisor ──────────────
        Users registrant;
        Users? advisor = null;

        if (isLecturer)
        {
            // Lecturer phải chỉ định thí sinh/đội trưởng
            if (string.IsNullOrWhiteSpace(model.LeaderStudentId))
            {
                ModelState.AddModelError(nameof(model.LeaderStudentId), "Vui lòng nhập MSSV của thí sinh/đội trưởng.");
            }

            var leaderUser = string.IsNullOrWhiteSpace(model.LeaderStudentId) ? null :
                await _context.Users
                    .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.StudentId == model.LeaderStudentId && u.IsActive);

            if (leaderUser == null && !string.IsNullOrWhiteSpace(model.LeaderStudentId))
                ModelState.AddModelError(nameof(model.LeaderStudentId), "Không tìm thấy thí sinh với MSSV này.");

            if (!ModelState.IsValid)
            {
                var vm = BuildRegistrationViewModel(competition, currentUser);
                vm.IsLecturerFlow = true;
                vm.RegistrationType = model.RegistrationType;
                vm.TeamName = model.TeamName;
                vm.Notes = model.Notes;
                return View("~/Views/Pages/StudentCompetitionRegistration.cshtml", vm);
            }

            registrant = leaderUser!;
            advisor = currentUser; // Lecturer = advisor
        }
        else
        {
            registrant = currentUser;

            // Tìm advisor nếu có
            if (!string.IsNullOrWhiteSpace(model.AdvisorStudentId))
            {
                advisor = await _context.Users
                    .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.StudentId == model.AdvisorStudentId && u.IsActive);
                if (advisor == null)
                    ModelState.AddModelError(nameof(model.AdvisorStudentId), "Không tìm thấy giảng viên với MSSV/mã này.");
                else if (!advisor.UserRoles.Any(ur => ur.Role.RoleName == "Lecturer"))
                    ModelState.AddModelError(nameof(model.AdvisorStudentId), "Người dùng này không phải Giảng viên.");
            }
        }

        // Kiểm tra thí sinh chưa đăng ký
        var alreadyRegistered = await _context.Registrations
            .AnyAsync(r => r.CompetitionId == id && r.UserId == registrant.UserId && r.Status != "Withdrawn");
        if (alreadyRegistered)
        {
            ModelState.AddModelError("", isLecturer
                ? "Thí sinh này đã đăng ký cuộc thi."
                : "Bạn đã đăng ký cuộc thi này rồi.");
        }

        var activeRound = competition.RegistrationRounds
            .FirstOrDefault(r => nowUtc >= r.StartDate && nowUtc <= r.EndDate);

        var currentCount = await _context.Registrations
            .CountAsync(r => r.CompetitionId == id && r.Status != "Withdrawn");

        if (currentCount >= competition.MaxParticipants)
            ModelState.AddModelError("", "Cuộc thi đã đạt số lượng đăng ký tối đa.");

        // ── Validate team ─────────────────────────────────────────────────
        List<Users> memberUsers = new();
        if (competition.IsTeamBased && model.RegistrationType == "Team")
        {
            if (string.IsNullOrWhiteSpace(model.TeamName))
                ModelState.AddModelError(nameof(model.TeamName), "Vui lòng nhập tên nhóm.");

            var validIds = (model.MemberStudentIds ?? new List<string>())
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Distinct()
                .ToList();

            // Tổng thành viên = trưởng nhóm (1) + các thành viên thêm
            var totalMembers = 1 + validIds.Count;
            if (competition.MaxTeamSize > 0 && totalMembers > competition.MaxTeamSize)
                ModelState.AddModelError(nameof(model.MemberStudentIds),
                    $"Số thành viên ({totalMembers}) vượt quá giới hạn {competition.MaxTeamSize}.");

            if (validIds.Any())
            {
                memberUsers = await _context.Users
                    .Where(u => validIds.Contains(u.StudentId!) && u.IsActive)
                    .ToListAsync();

                var notFound = validIds.Except(memberUsers.Select(u => u.StudentId!)).ToList();
                if (notFound.Any())
                    ModelState.AddModelError(nameof(model.MemberStudentIds),
                        $"Không tìm thấy MSSV: {string.Join(", ", notFound)}");
            }
        }

        if (!ModelState.IsValid)
        {
            var vm = BuildRegistrationViewModel(competition, currentUser);
            vm.IsLecturerFlow = isLecturer;
            vm.RegistrationType = model.RegistrationType;
            vm.TeamName = model.TeamName;
            vm.Notes = model.Notes;
            vm.AdvisorStudentId = model.AdvisorStudentId;
            vm.LeaderStudentId = model.LeaderStudentId;
            vm.MemberStudentIds = model.MemberStudentIds ?? new();
            return View("~/Views/Pages/StudentCompetitionRegistration.cshtml", vm);
        }

        // ── Upload files ──────────────────────────────────────────────────
        var uploadPaths = new List<string>();
        var uploadDir = Path.Combine(_environment.WebRootPath, "uploads", "registrations", $"competition-{id}");
        Directory.CreateDirectory(uploadDir);

        async Task SaveFile(IFormFile? file)
        {
            if (file == null || file.Length == 0) return;
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowed = new[] { ".pdf", ".doc", ".docx", ".zip", ".rar" };
            if (!allowed.Contains(ext))
                throw new InvalidOperationException("File không hợp lệ. Chỉ hỗ trợ PDF/DOC/DOCX/ZIP/RAR.");
            var fileName = $"{Guid.NewGuid():N}{ext}";
            await using var stream = new FileStream(Path.Combine(uploadDir, fileName), FileMode.Create);
            await file.CopyToAsync(stream);
            uploadPaths.Add($"/uploads/registrations/competition-{id}/{fileName}");
        }

        try
        {
            await SaveFile(model.CvFile);
            await SaveFile(model.ProposalFile);
            await SaveFile(model.ProjectFile);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            var vm = BuildRegistrationViewModel(competition, currentUser);
            vm.IsLecturerFlow = isLecturer;
            return View("~/Views/Pages/StudentCompetitionRegistration.cshtml", vm);
        }

        // ── Tạo Team ──────────────────────────────────────────────────────
        Teams? team = null;
        if (competition.IsTeamBased && model.RegistrationType == "Team")
        {
            team = new Teams
            {
                TeamName = model.TeamName!.Trim(),
                CompetitionId = competition.CompetitionId,
                LeaderId = registrant.UserId,
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };
            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            // Thêm đội trưởng vào TeamMembers
            _context.TeamMembers.Add(new TeamMembers
            {
                TeamId = team.TeamId,
                UserId = registrant.UserId,
                Role = "Leader",
                InvitedBy = isLecturer ? currentUser.UserId : null,
                JoinedAt = DateTime.UtcNow
            });

            // Thêm các thành viên khác
            foreach (var member in memberUsers)
            {
                _context.TeamMembers.Add(new TeamMembers
                {
                    TeamId = team.TeamId,
                    UserId = member.UserId,
                    Role = "Member",
                    InvitedBy = isLecturer ? currentUser.UserId : registrant.UserId,
                    JoinedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
        }

        // ── Tạo Registration ──────────────────────────────────────────────
        var registration = new Registrations
        {
            UserId = registrant.UserId,
            CompetitionId = competition.CompetitionId,
            TeamId = team?.TeamId,
            RoundId = activeRound?.RoundId,
            AdvisorId = advisor?.UserId,
            RegistrationType = team != null ? "Team" : "Individual",
            Status = "Pending",
            Notes = model.Notes?.Trim(),
            SubmissionDocument = uploadPaths.Count > 0 ? string.Join(";", uploadPaths) : null,
            RegistrationDate = DateTime.UtcNow
        };

        _context.Registrations.Add(registration);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng chờ xét duyệt.";
        return RedirectToAction("RegistrationDetail", new { id = competition.CompetitionId });
    }

    // GET: /Competitions/Registration/{id}
    [Authorize(Roles = "Student,Lecturer")]
    [HttpGet]
    [Route("Competitions/Registration/{id:int}")]
    public async Task<IActionResult> RegistrationDetail(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Index", "Login");

        var currentUserId = int.Parse(userId);
        var isLecturer = User.IsInRole("Lecturer");

        var registration = await _context.Registrations
            .Include(r => r.Competition)
            .Include(r => r.User)
            .Include(r => r.Advisor)
            .Include(r => r.RegistrationRound)
            .Include(r => r.Team)
                .ThenInclude(t => t!.TeamMembers)
                    .ThenInclude(tm => tm.User)
            .Include(r => r.Team)
                .ThenInclude(t => t!.TeamTasks.OrderByDescending(tt => tt.CreatedAt))
                    .ThenInclude(tt => tt.AssignedByUser)
            .Include(r => r.Team)
                .ThenInclude(t => t!.TeamTasks)
                    .ThenInclude(tt => tt.AssignedToUser)
            .Include(r => r.Team)
                .ThenInclude(t => t!.TeamTasks)
                    .ThenInclude(tt => tt.TaskCompletions)
                        .ThenInclude(tc => tc.CompletedByUser)
            .AsSplitQuery()
            .FirstOrDefaultAsync(r => r.CompetitionId == id
                && r.Status != "Withdrawn"
                && (isLecturer
                    ? r.AdvisorId == currentUserId
                    : r.UserId == currentUserId
                      || (r.TeamId != null && r.Team!.TeamMembers.Any(tm => tm.UserId == currentUserId))));

        if (registration == null)
            return RedirectToAction("Register", new { id });

        ViewBag.IsLecturerView = isLecturer;
        ViewBag.CurrentUserId  = currentUserId;
        return View("~/Views/Pages/RegistrationDetail.cshtml", registration);
    }

    // POST /Competitions/Registration/{id}/Edit — Thí sinh chỉnh sửa hồ sơ khi còn trong hạn
    [Authorize(Roles = "Student")]
    [HttpPost("/Competitions/Registration/{id:int}/Edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditRegistration(int id, [FromForm] EditRegistrationViewModel model)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr))
            return Unauthorized(new { message = "Bạn chưa đăng nhập." });

        var currentUserId = int.Parse(userIdStr);

        var registration = await _context.Registrations
            .Include(r => r.Competition)
                .ThenInclude(c => c.RegistrationRounds)
            .Include(r => r.RegistrationRound)
            .Include(r => r.Team)
            .Include(r => r.Advisor)
            .FirstOrDefaultAsync(r => r.CompetitionId == id
                && r.UserId == currentUserId
                && r.Status != "Withdrawn");

        if (registration == null)
            return NotFound(new { message = "Không tìm thấy hồ sơ đăng ký." });

        // Xác định hạn đăng ký
        var deadline = registration.RegistrationRound?.EndDate
            ?? (registration.Competition.RegistrationRounds.Any()
                ? registration.Competition.RegistrationRounds.Max(r => r.EndDate)
                : (DateTime?)null);

        var nowUtc = DateTime.UtcNow;
        if (deadline.HasValue && nowUtc > deadline.Value)
            return BadRequest(new { message = "Đã quá hạn đăng ký. Không thể chỉnh sửa hồ sơ." });
        if (!deadline.HasValue && nowUtc >= registration.Competition.StartDate)
            return BadRequest(new { message = "Cuộc thi đã bắt đầu. Không thể chỉnh sửa hồ sơ." });

        // Cập nhật ghi chú
        registration.Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim();

        // Cập nhật tên nhóm
        if (registration.Team != null && !string.IsNullOrWhiteSpace(model.TeamName))
            registration.Team.TeamName = model.TeamName.Trim();

        // Cập nhật giảng viên hướng dẫn
        if (model.AdvisorStudentId != null)
        {
            if (model.AdvisorStudentId.Trim() == "")
            {
                registration.AdvisorId = null;
            }
            else
            {
                var advisor = await _context.Users
                    .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.StudentId == model.AdvisorStudentId.Trim() && u.IsActive);

                if (advisor == null)
                    return BadRequest(new { message = "Không tìm thấy giảng viên với MSSV/mã này." });
                if (!advisor.UserRoles.Any(ur => ur.Role.RoleName == "Lecturer"))
                    return BadRequest(new { message = "Người dùng này không phải Giảng viên." });

                registration.AdvisorId = advisor.UserId;
            }
        }

        // Xử lý xóa file cũ
        var currentFiles = (registration.SubmissionDocument ?? "")
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(f => f.Trim())
            .ToList();

        if (model.RemoveFiles?.Any() == true)
            currentFiles = currentFiles.Where(f => !model.RemoveFiles.Contains(f)).ToList();

        // Upload file mới
        var uploadDir = Path.Combine(_environment.WebRootPath, "uploads", "registrations", $"competition-{id}");
        Directory.CreateDirectory(uploadDir);

        async Task<string?> SaveFileAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0) return null;
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowed = new[] { ".pdf", ".doc", ".docx", ".zip", ".rar" };
            if (!allowed.Contains(ext))
                throw new InvalidOperationException($"File '{file.FileName}' không hợp lệ. Chỉ hỗ trợ PDF/DOC/DOCX/ZIP/RAR.");
            var fileName = $"{Guid.NewGuid():N}{ext}";
            await using var stream = new FileStream(Path.Combine(uploadDir, fileName), FileMode.Create);
            await file.CopyToAsync(stream);
            return $"/uploads/registrations/competition-{id}/{fileName}";
        }

        try
        {
            foreach (var upload in new[] { model.NewFile1, model.NewFile2, model.NewFile3 })
            {
                var path = await SaveFileAsync(upload);
                if (path != null) currentFiles.Add(path);
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        registration.SubmissionDocument = currentFiles.Any() ? string.Join(";", currentFiles) : null;
        registration.UpdatedAt = nowUtc;

        // Reset về Pending nếu đã được duyệt/từ chối để ban tổ chức xem xét lại
        if (registration.Status is "Approved" or "Rejected")
            registration.Status = "Pending";

        await _context.SaveChangesAsync();
        return Ok(new { message = "Cập nhật hồ sơ đăng ký thành công!" });
    }

    // ── Task API ─────────────────────────────────────────────────────────────

    // POST /api/teams/{teamId}/tasks  — Lecturer tạo task
    [Authorize(Roles = "Lecturer")]
    [HttpPost("/api/teams/{teamId:int}/tasks")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTask(int teamId, [FromBody] CreateTaskRequest dto)
    {
        var lecturerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // Kiểm tra Lecturer là advisor của registration chứa teamId này
        var isAdvisor = await _context.Registrations
            .AnyAsync(r => r.TeamId == teamId && r.AdvisorId == lecturerId && r.Status != "Withdrawn");
        if (!isAdvisor)
            return Forbid();

        // Validate assignee là thành viên trong team
        if (dto.AssignedTo.HasValue)
        {
            var isMember = await _context.TeamMembers
                .AnyAsync(tm => tm.TeamId == teamId && tm.UserId == dto.AssignedTo.Value);
            if (!isMember)
                return BadRequest(new { message = "Người được giao không phải thành viên nhóm." });
        }

        var task = new TeamTasks
        {
            TeamId     = teamId,
            AssignedBy = lecturerId,
            AssignedTo = dto.AssignedTo,
            Title       = dto.Title.Trim(),
            Description = dto.Description?.Trim(),
            DueDate    = dto.DueDate.HasValue ? dto.DueDate.Value.ToUniversalTime() : null,
            Status     = "Pending",
            CreatedAt  = DateTime.UtcNow
        };
        _context.TeamTasks.Add(task);
        await _context.SaveChangesAsync();

        // Load related data để trả về
        await _context.Entry(task).Reference(t => t.AssignedByUser).LoadAsync();
        if (task.AssignedTo.HasValue)
            await _context.Entry(task).Reference(t => t.AssignedToUser).LoadAsync();

        return Ok(new
        {
            task.TaskId,
            task.Title,
            task.Description,
            task.Status,
            DueDate = task.DueDate,
            CreatedAt = task.CreatedAt,
            AssignedByName = task.AssignedByUser.FullName,
            AssignedToName = task.AssignedToUser?.FullName,
            AssignedToId   = task.AssignedTo
        });
    }

    // POST /api/tasks/{taskId}/complete  — Student xác nhận hoàn thành
    [Authorize(Roles = "Student")]
    [HttpPost("/api/tasks/{taskId:int}/complete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteTask(int taskId, [FromBody] CompleteTaskRequest dto)
    {
        var studentId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var task = await _context.TeamTasks
            .Include(t => t.TaskCompletions)
            .FirstOrDefaultAsync(t => t.TaskId == taskId);

        if (task == null) return NotFound(new { message = "Không tìm thấy task." });

        // Kiểm tra student là thành viên nhóm
        var isMember = await _context.TeamMembers
            .AnyAsync(tm => tm.TeamId == task.TeamId && tm.UserId == studentId);
        if (!isMember) return Forbid();

        // Kiểm tra task có được giao cho mình không (hoặc giao cho tất cả)
        if (task.AssignedTo.HasValue && task.AssignedTo != studentId)
            return Forbid();

        // Kiểm tra chưa complete
        if (task.TaskCompletions.Any(tc => tc.CompletedBy == studentId))
            return BadRequest(new { message = "Bạn đã đánh dấu hoàn thành task này rồi." });

        var completion = new TaskCompletions
        {
            TaskId      = taskId,
            CompletedBy = studentId,
            Note        = dto.Note?.Trim(),
            CompletedAt = DateTime.UtcNow
        };
        _context.TaskCompletions.Add(completion);

        // Cập nhật status task
        task.Status    = "Completed";
        task.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(new { completion.CompletionId, completion.CompletedAt, completion.Note });
    }

    // DELETE /api/tasks/{taskId}  — Lecturer xóa task
    [Authorize(Roles = "Lecturer")]
    [HttpPost("/api/tasks/{taskId:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTask(int taskId)
    {
        var lecturerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var task = await _context.TeamTasks
            .FirstOrDefaultAsync(t => t.TaskId == taskId && t.AssignedBy == lecturerId);

        if (task == null) return NotFound(new { message = "Không tìm thấy task hoặc bạn không có quyền xóa." });

        _context.TeamTasks.Remove(task);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Đã xóa task." });
    }

    // GET: /Competitions/{id}/Submit
    [Authorize(Roles = "Student,Lecturer")]
    [HttpGet("/Competitions/{id:int}/Submit")]
    public async Task<IActionResult> Submit(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return RedirectToAction("Index", "Login");

        var currentUserId = int.Parse(userId);
        var isLecturer = User.IsInRole("Lecturer");

        var competition = await _context.Competitions
            .Include(c => c.CompetitionRounds)
            .FirstOrDefaultAsync(c => c.CompetitionId == id);

        if (competition == null) return NotFound("Cuộc thi không tồn tại.");

        var registration = await _context.Registrations
            .Include(r => r.Team).ThenInclude(t => t!.TeamMembers).ThenInclude(tm => tm.User)
            .FirstOrDefaultAsync(r => r.CompetitionId == id
                && r.Status != "Withdrawn"
                && (isLecturer
                    ? r.AdvisorId == currentUserId
                    : r.UserId == currentUserId
                      || (r.TeamId != null && r.Team!.TeamMembers.Any(tm => tm.UserId == currentUserId))));

        if (registration == null)
        {
            TempData["ErrorMessage"] = "Bạn chưa đăng ký cuộc thi này.";
            return RedirectToAction("Details", new { id });
        }

        var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == currentUserId);
        var existing = await FindSubmissionAsync(id, registration);
        var vm = BuildSubmissionViewModel(competition, registration, currentUser!, existing);
        return View("~/Views/Pages/Submissions.cshtml", vm);
    }

    // POST: /Competitions/{id}/Submit
    [Authorize(Roles = "Student,Lecturer")]
    [HttpPost("/Competitions/{id:int}/Submit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(int id, SubmissionViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return RedirectToAction("Index", "Login");

        var currentUserId = int.Parse(userId);
        var isLecturer = User.IsInRole("Lecturer");

        var competition = await _context.Competitions
            .Include(c => c.CompetitionRounds)
            .FirstOrDefaultAsync(c => c.CompetitionId == id);

        if (competition == null) return NotFound("Cuộc thi không tồn tại.");

        var registration = await _context.Registrations
            .Include(r => r.Team).ThenInclude(t => t!.TeamMembers).ThenInclude(tm => tm.User)
            .FirstOrDefaultAsync(r => r.CompetitionId == id
                && r.Status != "Withdrawn"
                && (isLecturer
                    ? r.AdvisorId == currentUserId
                    : r.UserId == currentUserId
                      || (r.TeamId != null && r.Team!.TeamMembers.Any(tm => tm.UserId == currentUserId))));

        var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == currentUserId);

        if (registration == null || registration.Status != "Approved")
        {
            TempData["ErrorMessage"] = "Đăng ký chưa được duyệt hoặc không hợp lệ.";
            return RedirectToAction("Submit", new { id });
        }

        if (DateTime.UtcNow > competition.SubmissionDeadline)
        {
            TempData["ErrorMessage"] = "Đã hết hạn nộp bài.";
            return RedirectToAction("Submit", new { id });
        }

        if (string.IsNullOrWhiteSpace(model.Title))
            ModelState.AddModelError(nameof(model.Title), "Tiêu đề sản phẩm không được để trống.");

        if (!ModelState.IsValid)
        {
            var existing0 = await FindSubmissionAsync(id, registration);
            var vm0 = BuildSubmissionViewModel(competition, registration, currentUser!, existing0);
            vm0.Title = model.Title;
            vm0.Description = model.Description;
            vm0.SelectedRoundId = model.SelectedRoundId;
            vm0.VideoUrl = model.VideoUrl;
            vm0.ProjectLink = model.ProjectLink;
            return View("~/Views/Pages/Submissions.cshtml", vm0);
        }

        var existing = await FindSubmissionAsync(id, registration);
        string? fileUrl = existing?.FileUrl;

        if (model.SubmissionFile != null && model.SubmissionFile.Length > 0)
        {
            var ext = Path.GetExtension(model.SubmissionFile.FileName).ToLowerInvariant();
            var allowed = new[] { ".pdf", ".doc", ".docx", ".zip", ".rar" };
            if (!allowed.Contains(ext))
            {
                ModelState.AddModelError(nameof(model.SubmissionFile), "File không hợp lệ. Chỉ hỗ trợ PDF, DOC, DOCX, ZIP, RAR.");
                var vm1 = BuildSubmissionViewModel(competition, registration, currentUser!, existing);
                vm1.Title = model.Title;
                vm1.Description = model.Description;
                return View("~/Views/Pages/Submissions.cshtml", vm1);
            }

            var uploadDir = Path.Combine(_environment.WebRootPath, "uploads", "submissions", $"competition-{id}");
            Directory.CreateDirectory(uploadDir);
            var fileName = $"{Guid.NewGuid():N}{ext}";
            await using var stream = new FileStream(Path.Combine(uploadDir, fileName), FileMode.Create);
            await model.SubmissionFile.CopyToAsync(stream);
            fileUrl = $"/uploads/submissions/competition-{id}/{fileName}";
        }

        var newStatus = model.SubmitAction == "submit" ? "Submitted" : "Draft";

        if (existing != null)
        {
            if (existing.Status is "Under Review" or "Evaluated")
            {
                TempData["ErrorMessage"] = "Bài đang trong quá trình chấm điểm, không thể chỉnh sửa.";
                return RedirectToAction("Submit", new { id });
            }

            existing.Title = model.Title.Trim();
            existing.Description = model.Description?.Trim();
            existing.FileUrl = fileUrl;
            existing.VideoUrl = string.IsNullOrWhiteSpace(model.VideoUrl) ? null : model.VideoUrl.Trim();
            existing.ProjectLink = string.IsNullOrWhiteSpace(model.ProjectLink) ? null : model.ProjectLink.Trim();
            existing.CompetitionRoundId = model.SelectedRoundId;
            existing.Status = newStatus;
            existing.UpdatedAt = DateTime.UtcNow;
            if (newStatus == "Submitted") existing.SubmissionDate = DateTime.UtcNow;
        }
        else
        {
            var submission = new Submissions
            {
                CompetitionId = id,
                RegistrationId = registration.TeamId.HasValue ? null : registration.RegistrationId,
                TeamId = registration.TeamId,
                CompetitionRoundId = model.SelectedRoundId,
                Title = model.Title.Trim(),
                Description = model.Description?.Trim(),
                FileUrl = fileUrl,
                VideoUrl = string.IsNullOrWhiteSpace(model.VideoUrl) ? null : model.VideoUrl.Trim(),
                ProjectLink = string.IsNullOrWhiteSpace(model.ProjectLink) ? null : model.ProjectLink.Trim(),
                Status = newStatus,
                SubmissionDate = DateTime.UtcNow
            };
            _context.Submissions.Add(submission);
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = newStatus == "Submitted" ? "Nộp bài thành công!" : "Đã lưu nháp thành công!";
        return RedirectToAction("Submit", new { id });
    }

    private async Task<Submissions?> FindSubmissionAsync(int competitionId, Registrations registration)
    {
        if (registration.TeamId.HasValue)
            return await _context.Submissions.FirstOrDefaultAsync(s =>
                s.CompetitionId == competitionId && s.TeamId == registration.TeamId);
        return await _context.Submissions.FirstOrDefaultAsync(s =>
            s.CompetitionId == competitionId && s.RegistrationId == registration.RegistrationId);
    }

    private SubmissionViewModel BuildSubmissionViewModel(
        Competitions competition, Registrations registration, Users user, Submissions? existing)
    {
        var rounds = competition.CompetitionRounds
            .OrderBy(r => r.RoundOrder)
            .Select(r => new SubmissionRoundDto
            {
                RoundId = r.RoundId,
                RoundName = r.RoundName,
                SubmissionDeadline = r.SubmissionDeadline,
                Status = r.Status,
                RoundOrder = r.RoundOrder
            }).ToList();

        var members = registration.Team?.TeamMembers
            .Select(tm => new SubmissionTeamMemberDto
            {
                UserId = tm.UserId,
                FullName = tm.User?.FullName ?? "N/A",
                StudentId = tm.User?.StudentId,
                Role = tm.Role
            }).ToList() ?? new();

        return new SubmissionViewModel
        {
            CompetitionId = competition.CompetitionId,
            CompetitionName = competition.CompetitionName,
            Category = competition.Category,
            SubmissionDeadline = competition.SubmissionDeadline,
            IsTeamBased = competition.IsTeamBased,
            RegistrationId = registration.RegistrationId,
            RegistrationStatus = registration.Status,
            TeamName = registration.Team?.TeamName,
            TeamId = registration.TeamId,
            TeamMembers = members,
            AvailableRounds = rounds,
            ExistingSubmissionId = existing?.SubmissionId,
            ExistingStatus = existing?.Status,
            ExistingSubmissionDate = existing?.SubmissionDate,
            ExistingFileUrl = existing?.FileUrl,
            Title = existing?.Title ?? string.Empty,
            Description = existing?.Description,
            SelectedRoundId = existing?.CompetitionRoundId,
            VideoUrl = existing?.VideoUrl,
            ProjectLink = existing?.ProjectLink,
            UserFullName = user.FullName,
            StudentId = user.StudentId ?? string.Empty
        };
    }

    private CompetitionRegistrationViewModel BuildRegistrationViewModel(Competitions competition, Users user)
    {
        var latestDeadline = competition.RegistrationRounds.Any()
            ? competition.RegistrationRounds.Max(r => r.EndDate)
            : (DateTime?)null;

        return new CompetitionRegistrationViewModel
        {
            CompetitionId = competition.CompetitionId,
            CompetitionName = competition.CompetitionName,
            Category = competition.Category ?? "Chưa xác định",
            Description = competition.Description,
            LatestRegistrationDeadline = latestDeadline,
            SubmissionDeadline = competition.SubmissionDeadline,
            IsTeamBased = competition.IsTeamBased,
            MaxTeamSize = competition.MaxTeamSize,
            MaxParticipants = competition.MaxParticipants,
            FullName = user.FullName,
            StudentId = user.StudentId ?? string.Empty,
            Email = user.Email,
        };
    }

    // GET: /MyRegistrations
    [Authorize(Roles = "Student,Lecturer")]
    [HttpGet("/MyRegistrations")]
    public async Task<IActionResult> MyRegistrations()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr))
            return RedirectToAction("Index", "Login");

        var userId = int.Parse(userIdStr);
        var isLecturer = User.IsInRole("Lecturer");

        var registrations = await _context.Registrations
            .Include(r => r.Competition)
                .ThenInclude(c => c.CompetitionImages)
            .Include(r => r.Team)
                .ThenInclude(t => t!.TeamMembers)
                    .ThenInclude(tm => tm.User)
            .Include(r => r.User)
            .Include(r => r.Advisor)
            .Where(r => r.Status != "Withdrawn"
                && (isLecturer
                    ? r.AdvisorId == userId
                    : r.UserId == userId
                      || (r.TeamId != null && r.Team!.TeamMembers.Any(tm => tm.UserId == userId))))
            .OrderByDescending(r => r.RegistrationDate)
            .ToListAsync();

        ViewBag.IsLecturerView = isLecturer;
        return View("~/Views/Pages/ListStudentConpetitions.cshtml", registrations);
    }

    // GET: /Results
    [HttpGet("/Results")]
    public async Task<IActionResult> Results(
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] string? category = null)
    {
        var competitionsQuery = _context.Competitions
            .Include(c => c.CompetitionImages)
            .Include(c => c.CompetitionRounds.OrderBy(r => r.RoundOrder))
                .ThenInclude(r => r.ScoringCriteria.OrderBy(sc => sc.Order))
            .Where(c => c.Status != "Draft");

        if (!string.IsNullOrWhiteSpace(search))
            competitionsQuery = competitionsQuery.Where(c => c.CompetitionName.Contains(search));

        if (!string.IsNullOrWhiteSpace(status) && status != "all")
            competitionsQuery = competitionsQuery.Where(c => c.Status == status);

        if (!string.IsNullOrWhiteSpace(category) && category != "all")
            competitionsQuery = competitionsQuery.Where(c => c.Category == category);

        var competitions = await competitionsQuery
            .AsSplitQuery()
            .OrderByDescending(c => c.StartDate)
            .ToListAsync();

        var competitionIds = competitions.Select(c => c.CompetitionId).ToList();

        // Chỉ tính điểm đã được trưởng ban duyệt (Approved)
        var submissions = await _context.Submissions
            .Include(s => s.Scores.Where(sc => sc.ApprovalStatus == "Approved"))
                .ThenInclude(sc => sc.Criteria)
            .Include(s => s.Team)
                .ThenInclude(t => t!.Leader)
            .Include(s => s.Registration)
                .ThenInclude(r => r!.User)
            .Where(s => competitionIds.Contains(s.CompetitionId)
                     && s.Status != "Draft"
                     && s.CompetitionRoundId != null)
            .AsSplitQuery()
            .ToListAsync();

        var submissionsByRound = submissions
            .GroupBy(s => (s.CompetitionId, s.CompetitionRoundId))
            .ToDictionary(g => g.Key, g => g.ToList());

        var categories = await _context.Competitions
            .Where(c => c.Status != "Draft" && c.Category != null)
            .Select(c => c.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        var results = competitions.Select(competition =>
        {
            var rounds = competition.CompetitionRounds
                .OrderBy(r => r.RoundOrder)
                .Select(round =>
                {
                    var criteria = round.ScoringCriteria
                        .OrderBy(c => c.Order)
                        .Select(c => new PublicScoringCriteriaDto
                        {
                            CriteriaId = c.CriteriaId,
                            CriteriaName = c.CriteriaName,
                            MaxScore = c.MaxScore,
                            Weight = c.Weight
                        }).ToList();

                    var key = (competition.CompetitionId, (int?)round.RoundId);
                    var roundSubmissions = submissionsByRound.GetValueOrDefault(key, new())
                        .Where(s => s.Scores.Any())  // chỉ hiện bài có ít nhất 1 điểm
                        .ToList();

                    var rankings = roundSubmissions.Select(submission =>
                    {
                        var approvedScores = submission.Scores.ToList();
                        var criteriaScores = new Dictionary<int, decimal>();
                        var criteriaAvg = approvedScores
                            .GroupBy(s => s.CriteriaId)
                            .ToDictionary(g => g.Key, g => g.Average(s => s.Score));

                        decimal totalWeighted = 0;
                        decimal maxPossible = 0;
                        foreach (var c in criteria)
                        {
                            var avg = criteriaAvg.GetValueOrDefault(c.CriteriaId, 0);
                            criteriaScores[c.CriteriaId] = Math.Round(avg, 2);
                            totalWeighted += avg * c.Weight;
                            maxPossible += c.MaxScore * c.Weight;
                        }

                        return new PublicSubmissionRankingDto
                        {
                            SubmissionId = submission.SubmissionId,
                            Title = submission.Title,
                            IsTeam = submission.TeamId.HasValue,
                            ParticipantName = submission.Team?.TeamName
                                ?? submission.Registration?.User?.FullName
                                ?? "N/A",
                            StudentId = submission.Team?.Leader?.StudentId
                                ?? submission.Registration?.User?.StudentId,
                            TotalScore = Math.Round(totalWeighted, 2),
                            MaxPossibleScore = Math.Round(maxPossible, 2),
                            ScorePercentage = maxPossible > 0 ? Math.Round(totalWeighted / maxPossible * 100, 1) : 0,
                            CriteriaScores = criteriaScores,
                            JudgeCount = approvedScores.Select(s => s.JudgeId).Distinct().Count()
                        };
                    })
                    .OrderByDescending(r => r.TotalScore)
                    .ToList();

                    for (int i = 0; i < rankings.Count; i++)
                        rankings[i].Rank = i + 1;

                    return new PublicRoundResultDto
                    {
                        RoundId = round.RoundId,
                        RoundName = round.RoundName,
                        RoundOrder = round.RoundOrder,
                        Status = round.Status,
                        Criteria = criteria,
                        Rankings = rankings
                    };
                }).ToList();

            return new PublicCompetitionResultDto
            {
                CompetitionId = competition.CompetitionId,
                CompetitionName = competition.CompetitionName,
                Category = competition.Category,
                Status = competition.Status,
                StartDate = competition.StartDate,
                EndDate = competition.EndDate,
                Prize = competition.Prize,
                ThumbnailUrl = competition.CompetitionImages.FirstOrDefault(i => i.IsThumbnail)?.ImageUrl
                              ?? competition.CompetitionImages.FirstOrDefault()?.ImageUrl,
                TotalEvaluatedSubmissions = rounds.Sum(r => r.Rankings.Count),
                Rounds = rounds
            };
        }).ToList();

        var vm = new PublicResultsViewModel
        {
            SearchQuery = search,
            StatusFilter = status,
            CategoryFilter = category,
            AvailableCategories = categories,
            Results = results,
            TotalCompetitions = competitions.Count,
            TotalRankedRounds = results.Sum(r => r.Rounds.Count(ro => ro.Rankings.Any()))
        };

        return View("~/Views/Pages/Results.cshtml", vm);
    }

    // GET: /HuongDan
    [HttpGet("/HuongDan")]
    public IActionResult HuongDan()
    {
        return View("~/Views/Pages/HuongDan.cshtml");
    }

    // GET: /Competitions
    [HttpGet("/Competitions")]
    public async Task<IActionResult> Index()
    {
        try
        {
            var competitions = await _competitionService.GetAllCompetitionsAsync();
            return View("~/Views/Pages/Competitions.cshtml", competitions);
        }
        catch (Exception ex)
        {
            return View("Error", new { message = "Lỗi khi tải danh sách cuộc thi: " + ex.Message });
        
        }
    }

    // GET: /Competitions/{id}
    [HttpGet("/Competitions/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var competition = await _context.Competitions
            .Include(c => c.Registrations)
            .Include(c => c.Submissions)
            .Include(c => c.CompetitionImages)
            .Include(c => c.CompetitionDocuments)
            .Include(c => c.RegistrationRounds)
            .Include(c => c.CompetitionRounds)
                .ThenInclude(r => r.ScoringCriteria)
            .Include(c => c.Teams)
                .ThenInclude(t => t.Registrations)
            .Include(c => c.Teams)
                .ThenInclude(t => t.Leader)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.CompetitionId == id);

        if (competition == null)
            return NotFound("Cuộc thi không tồn tại.");

        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdValue, out var userId))
        {
            var registration = await _context.Registrations
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.CompetitionId == id && r.UserId == userId && r.Status != "Withdrawn");

            ViewBag.IsAlreadyRegistered = registration != null;
            ViewBag.RegistrationStatus = registration?.Status;
        }

        return View("~/Views/Pages/StudentCompetitionDetails.cshtml", competition);
    }


}

// ── Request DTOs for Task API ─────────────────────────────────────────────────
public record CreateTaskRequest(string Title, string? Description, int? AssignedTo, DateTime? DueDate);
public record CompleteTaskRequest(string? Note);