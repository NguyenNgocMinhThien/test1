using Microsoft.AspNetCore.Mvc;
using Web_cham_diem.Services;

namespace Web_cham_diem.Controllers;

public class CompetitiveController : Controller
{
    private readonly ICompetitionService _competitionService;

    public CompetitiveController(ICompetitionService competitionService)
    {
        _competitionService = competitionService;
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

    // Route alias cho /Competitions
    [Route("Competitions")]
    public async Task<IActionResult> Competitions()
    {
        return await Index();
    }
}