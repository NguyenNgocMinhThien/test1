using Microsoft.AspNetCore.Mvc;
using Web_cham_diem.Models;

namespace Web_cham_diem.Controllers;

public class RegisterTopicController : Controller
{
    private readonly ApplicationDbContext _context;

    public RegisterTopicController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Index(int? competitionId)
    {
        var model = new RegisterTopicViewModel();

        // N?u có ch?n cu?c thi t? trang trý?c
        if (competitionId.HasValue)
        {
            var competition = _context.Competitions.FirstOrDefault(c => c.CompetitionId == competitionId);
            if (competition != null)
            {
                model.CompetitionId = competitionId.Value;
                model.Competition = new CompetitionDetailViewModel
                {
                    CompetitionId = competition.CompetitionId,
                    CompetitionName = competition.CompetitionName,
                    Description = competition.Description,
                    Category = competition.Category,
                    RegistrationDeadline = competition.RegistrationDeadline,
                    SubmissionDeadline = competition.SubmissionDeadline,
                    MaxTeamSize = competition.MaxTeamSize,
                    IsTeamBased = competition.IsTeamBased,
                    Status = competition.Status
                };
            }
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(RegisterTopicViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // X? l? upload file
        string? uploadedFileName = null;
        if (model.SubmissionFile != null && model.SubmissionFile.Length > 0)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            Directory.CreateDirectory(uploadsFolder);
            uploadedFileName = $"{Guid.NewGuid()}_{model.SubmissionFile.FileName}";
            var filePath = Path.Combine(uploadsFolder, uploadedFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await model.SubmissionFile.CopyToAsync(fileStream);
            }
        }

        // T?o b?n ghi ðãng k?
        var registration = new Registrations
        {
            CompetitionId = model.CompetitionId,
            RegistrationType = model.RegistrationType,
            SubmissionDocument = uploadedFileName,
            Notes = model.Notes,
            Status = "Pending",
            RegistrationDate = DateTime.UtcNow
        };

        _context.Registrations.Add(registration);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Ðãng k? tham gia cu?c thi thành công! H? sõ c?a b?n ðang ch? xét duy?t.";
        return RedirectToAction("Success");
    }

    [HttpGet]
    public IActionResult Success()
    {
        return View();
    }

    [HttpGet]
    public IActionResult GetCompetitions()
    {
        var competitions = _context.Competitions
            .Where(c => c.Status == "Active")
            .Select(c => new { c.CompetitionId, c.CompetitionName, c.Category })
            .ToList();

        return Json(competitions);
    }
}
