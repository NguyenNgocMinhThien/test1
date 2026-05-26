using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Web_cham_diem.Models;
using Web_cham_diem.Services;

namespace Web_cham_diem.Controllers;

public class CompetitiveController : Controller
{
    private readonly ICompetitionService _competitionService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CompetitiveController> _logger;

    public CompetitiveController(
        ICompetitionService competitionService,
        ApplicationDbContext context,
        ILogger<CompetitiveController> logger)
    {
        _competitionService = competitionService;
        _context = context;
        _logger = logger;
    }

    // GET: /Competitions
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
    public async Task<IActionResult> Details(int id)
    {
        var competition = await _competitionService.GetCompetitionByIdAsync(id);

        if (competition == null)
            return NotFound("Cuộc thi không tồn tại");

        return View(competition);
    }

    // GET: /api/competitions - API endpoint cho AJAX
    [HttpGet]
    [Route("api/competitions")]
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

    // GET: /api/competitions/{id}/teams
    [HttpGet]
    [Route("api/competitions/{id}/teams")]
    public async Task<IActionResult> GetCompetitionTeams(int id)
    {
        try
        {
            var teams = await _competitionService.GetCompetitionTeamsAsync(id);
            var result = teams.Select(t => new
            {
                t.TeamId,
                t.TeamName,
                t.Description,
                t.Status,
                t.CreatedAt,
                leaderName = t.Leader.FullName,
                memberCount = t.Registrations.Count
            }).ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Lỗi khi tải dữ liệu", message = ex.Message });
        }
    }

    // GET: /api/competitions/{id}/registrations
    [HttpGet]
    [Route("api/competitions/{id}/registrations")]
    public async Task<IActionResult> GetCompetitionRegistrations(int id)
    {
        try
        {
            var registrations = await _competitionService.GetCompetitionRegistrationsAsync(id);
            var result = registrations.Select(r => new
            {
                r.RegistrationId,
                r.CompetitionId,
                r.TeamId,
                r.RegistrationType,
                r.Status,
                r.RegistrationDate,
                r.ApprovalDate,
                userName = r.User.FullName,
                userEmail = r.User.Email,
                teamName = r.Team != null ? r.Team.TeamName : "Cá nhân"
            }).ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Lỗi khi tải dữ liệu", message = ex.Message });
        }
    }


    [Authorize(Roles = "Organizer")]
    [HttpGet]
    public async Task<IActionResult> Rules(int? id = null)
    {
        await LoadCompetitionOptionsAsync(id);

        if (id.HasValue)
        {
            var competition = await _context.Competitions.FirstOrDefaultAsync(c => c.CompetitionId == id.Value);
            if (competition == null)
                return NotFound("Cuộc thi không tồn tại.");

            // Trả về view với đường dẫn đầy đủ
            return View("~/Views/Pages/Rules.cshtml", competition);
        }

        // Trả về view với model mới nếu không có id
        return View("~/Views/Pages/Rules.cshtml", new Competitions());
    }

    [Authorize(Roles = "Organizer")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rules(Competitions model)
    {
        if (model.CompetitionId <= 0)
            ModelState.AddModelError(nameof(model.CompetitionId), "Vui lòng chọn cuộc thi.");

        if (string.IsNullOrWhiteSpace(model.Rules))
            ModelState.AddModelError(nameof(model.Rules), "Vui lòng nhập luật lệ.");

        var competition = await _context.Competitions
            .FirstOrDefaultAsync(c => c.CompetitionId == model.CompetitionId);

        if (competition == null)
            ModelState.AddModelError(string.Empty, "Không tìm thấy cuộc thi.");

        if (!ModelState.IsValid)
        {
            await LoadCompetitionOptionsAsync(model.CompetitionId);
            // Trả về cùng view với đường dẫn đầy đủ khi có lỗi
            return View("~/Views/Pages/Rules.cshtml", model);
        }

        competition!.Rules = model.Rules?.Trim();
        competition.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đã lưu luật lệ cuộc thi thành công.";
        _logger.LogInformation("Organizer updated rules for competition {CompetitionId}", model.CompetitionId);

        return RedirectToAction(nameof(Rules), new { id = model.CompetitionId });
    }

    private async Task LoadCompetitionOptionsAsync(int? selectedId = null)
    {
        var competitions = await _context.Competitions
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        ViewBag.CompetitionOptions = new SelectList(
            competitions,
            "CompetitionId",
            "CompetitionName",
            selectedId);
    }

    // Route alias cho /Competitions
    [Route("Competitions")]
    public async Task<IActionResult> Competitions()
    {
        return await Index();
    }

    // Đường dẫn: /Competitive/Register
    public IActionResult Register()
    {
        // Chúng ta trả về đúng đường dẫn file bạn vừa tạo
        return View("~/Views/Pages/StudentCompetitionRegistration.cshtml");
    }
}