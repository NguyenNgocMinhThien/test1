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

    public CompetitiveController(
        ICompetitionService competitionService,
        ApplicationDbContext context,
        ILogger<CompetitiveController> logger,
        IWebHostEnvironment environment)
    {
        _competitionService = competitionService;
        _context = context;
        _logger = logger;
        _environment = environment;
    }

    // GET: /api/competitions
    [HttpGet("/api/competitions")]
    public async Task<IActionResult> GetCompetitions()
    {
        try
        {
            var competitions = await _competitionService.GetAllCompetitionsAsync();
            var result = competitions.Select(c => new
            {
                c.CompetitionId,
                c.CompetitionName,
                c.Description,
                c.Category,
                c.StartDate,
                c.EndDate,
                c.RegistrationDeadline,
                c.SubmissionDeadline,
                c.MaxParticipants,
                c.MaxTeamSize,
                c.MaxScore,
                c.Status,
                c.IsTeamBased,
                c.Prize,
                c.CreatedAt
            }).ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Lỗi khi tải dữ liệu", message = ex.Message });
        }
    }

    // GET: /Competitions/Register/{id}
    [Authorize(Roles = "Student")]
    [HttpGet]
    [Route("Competitions/Register/{id:int}")]
    public async Task<IActionResult> Register(int id)
    {
        var competition = await _context.Competitions.FirstOrDefaultAsync(c => c.CompetitionId == id);
        if (competition == null)
            return NotFound("Cuộc thi không tồn tại.");

        if (competition.Status != "Active" || DateTime.UtcNow > competition.RegistrationDeadline)
        {
            TempData["ErrorMessage"] = "Cuộc thi đã đóng đăng ký hoặc không còn hiệu lực.";
            return RedirectToAction("Details", new { id });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Index", "Login");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == int.Parse(userId));
        if (user == null)
            return RedirectToAction("Index", "Login");

        var isRegistered = await _context.Registrations
            .AnyAsync(r => r.CompetitionId == id && r.UserId == user.UserId && r.Status != "Withdrawn");

        if (isRegistered)
        {
            return RedirectToAction("RegistrationDetail", new { id });
        }

        var viewModel = BuildRegistrationViewModel(competition, user);
        return View("~/Views/Pages/StudentCompetitionRegistration.cshtml", viewModel);
    }

    // POST: /Competitions/Register/{id}
    [Authorize(Roles = "Student")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Competitions/Register/{id:int}")]
    public async Task<IActionResult> Register(int id, CompetitionRegistrationViewModel model)
    {
        var competition = await _context.Competitions.FirstOrDefaultAsync(c => c.CompetitionId == id);
        if (competition == null)
            return NotFound("Cuộc thi không tồn tại.");

        if (competition.Status != "Active" || DateTime.UtcNow > competition.RegistrationDeadline)
        {
            TempData["ErrorMessage"] = "Cuộc thi đã đóng đăng ký hoặc không còn hiệu lực.";
            return RedirectToAction("Details", new { id });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Index", "Login");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == int.Parse(userId));
        if (user == null)
            return RedirectToAction("Index", "Login");

        var isRegistered = await _context.Registrations
            .AnyAsync(r => r.CompetitionId == id && r.UserId == user.UserId && r.Status != "Withdrawn");

        if (isRegistered)
        {
            return RedirectToAction("RegistrationDetail", new { id });
        }

        var currentCount = await _context.Registrations
            .CountAsync(r => r.CompetitionId == id && r.Status != "Withdrawn");

        if (currentCount >= competition.MaxParticipants)
        {
            ModelState.AddModelError("", "Cuộc thi đã đạt số lượng đăng ký tối đa.");
        }

        if (competition.IsTeamBased && model.RegistrationType == "Team")
        {
            if (string.IsNullOrWhiteSpace(model.TeamName))
                ModelState.AddModelError(nameof(model.TeamName), "Vui lòng nhập tên nhóm.");

            var members = (model.TeamMembers ?? string.Empty)
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(m => m.Trim())
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .ToList();

            var totalMembers = 1 + members.Count; // +1 trưởng nhóm
            if (competition.MaxTeamSize > 0 && totalMembers > competition.MaxTeamSize)
            {
                ModelState.AddModelError(nameof(model.TeamMembers), $"Số thành viên vượt quá giới hạn {competition.MaxTeamSize}.");
            }
        }

        if (!ModelState.IsValid)
        {
            var vm = BuildRegistrationViewModel(competition, user);
            vm.RegistrationType = model.RegistrationType;
            vm.TeamName = model.TeamName;
            vm.TeamMembers = model.TeamMembers;
            vm.Notes = model.Notes;
            return View("~/Views/Pages/StudentCompetitionRegistration.cshtml", vm);
        }

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
            var filePath = Path.Combine(uploadDir, fileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
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
            var vm = BuildRegistrationViewModel(competition, user);
            return View("~/Views/Pages/StudentCompetitionRegistration.cshtml", vm);
        }

        Teams? team = null;
        if (competition.IsTeamBased && model.RegistrationType == "Team")
        {
            team = new Teams
            {
                TeamName = model.TeamName!.Trim(),
                CompetitionId = competition.CompetitionId,
                LeaderId = user.UserId,
                Description = model.TeamMembers,
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };
            _context.Teams.Add(team);
            await _context.SaveChangesAsync();
        }

        var registration = new Registrations
        {
            UserId = user.UserId,
            CompetitionId = competition.CompetitionId,
            TeamId = team?.TeamId,
            RegistrationType = team != null ? "Team" : "Individual",
            Status = "Pending",
            Notes = model.Notes?.Trim(),
            SubmissionDocument = uploadPaths.Count > 0 ? string.Join(";", uploadPaths) : null,
            RegistrationDate = DateTime.UtcNow
        };

        _context.Registrations.Add(registration);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng chờ xét duyệt.";
        return RedirectToAction("Details", new { id = competition.CompetitionId });
    }

    // GET: /Competitions/Registration/{id}
    [Authorize(Roles = "Student")]
    [HttpGet]
    [Route("Competitions/Registration/{id:int}")]
    public async Task<IActionResult> RegistrationDetail(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Index", "Login");

        var registration = await _context.Registrations
            .Include(r => r.Competition)
            .Include(r => r.User)
            .Include(r => r.Team)
            .FirstOrDefaultAsync(r => r.CompetitionId == id && r.UserId == int.Parse(userId) && r.Status != "Withdrawn");

        if (registration == null)
            return RedirectToAction("Register", new { id });

        return View("~/Views/Pages/RegistrationDetail.cshtml", registration);
    }

    private CompetitionRegistrationViewModel BuildRegistrationViewModel(Competitions competition, Users user)
    {
        return new CompetitionRegistrationViewModel
        {
            CompetitionId = competition.CompetitionId,
            CompetitionName = competition.CompetitionName,
            Category = competition.Category ?? "Chưa xác định",
            Description = competition.Description,
            RegistrationDeadline = competition.RegistrationDeadline,
            SubmissionDeadline = competition.SubmissionDeadline,
            IsTeamBased = competition.IsTeamBased,
            MaxTeamSize = competition.MaxTeamSize,
            MaxParticipants = competition.MaxParticipants,
            FullName = user.FullName,
            StudentId = user.StudentId ?? string.Empty,
            Email = user.Email,
  
        };
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
            .Include(c => c.ScoringCriteria)
            .Include(c => c.Registrations)
            .Include(c => c.Submissions)
            .Include(c => c.Teams)
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