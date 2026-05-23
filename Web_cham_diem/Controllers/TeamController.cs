using Microsoft.AspNetCore.Mvc;
using Web_cham_diem.Services;

namespace Web_cham_diem.Controllers;

public class TeamController : Controller
{
    private readonly ITeamService _teamService;

    public TeamController(ITeamService teamService)
    {
        _teamService = teamService;
    }

    // GET: /Teams hoặc /TeamRegistrations
    public async Task<IActionResult> Index()
    {
        try
        {
            var competitions = await _teamService.GetAllCompetitionsAsync();
            return View("~/Views/Pages/TeamRegistrations.cshtml", competitions);
        }
        catch (Exception ex)
        {
            return View("Error", new { message = "Lỗi khi tải danh sách: " + ex.Message });
        }
    }

    // GET: /Teams/{id}
    public async Task<IActionResult> Details(int id)
    {
        var team = await _teamService.GetTeamByIdAsync(id);

        if (team == null)
            return NotFound("Đội thi không tồn tại");

        return View(team);
    }

    // GET: /api/teams
    [HttpGet]
    [Route("api/teams")]
    public async Task<IActionResult> GetTeams()
    {
        try
        {
            var teams = await _teamService.GetAllTeamsAsync();
            var result = teams.Select(t => new
            {
                t.TeamId,
                t.TeamName,
                t.Description,
                t.Status,
                t.CreatedAt,
                t.CompetitionId,
                competitionName = t.Competition.CompetitionName,
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

    // GET: /api/teams/{id}
    [HttpGet]
    [Route("api/teams/{id}")]
    public async Task<IActionResult> GetTeamDetails(int id)
    {
        try
        {
            var team = await _teamService.GetTeamByIdAsync(id);

            if (team == null)
                return StatusCode(StatusCodes.Status404NotFound,
                    new { error = "Đội thi không tồn tại" });

            var result = new
            {
                team.TeamId,
                team.TeamName,
                team.Description,
                team.Status,
                team.CreatedAt,
                team.CompetitionId,
                competitionName = team.Competition.CompetitionName,
                leaderName = team.Leader.FullName,
                members = team.Registrations.Select(r => new
                {
                    r.RegistrationId,
                    userName = r.User.FullName,
                    userEmail = r.User.Email,
                    r.RegistrationDate,
                    r.Status
                })
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Lỗi khi tải dữ liệu", message = ex.Message });
        }
    }

    // Route alias cho /Teams
    [Route("Teams")]
    public async Task<IActionResult> Teams()
    {
        return await Index();
    }

    // Route alias cho /TeamRegistrations
    [Route("TeamRegistrations")]
    public async Task<IActionResult> TeamRegistrations()
    {
        return await Index();
    }
}