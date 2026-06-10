using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web_cham_diem.Models;
using Web_cham_diem.Models.ViewModels;

namespace Web_cham_diem.Controllers;

[Authorize(Roles = "Judge")]
public class JudgeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<JudgeController> _logger;

    public JudgeController(ApplicationDbContext context, ILogger<JudgeController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET /Judge/Dashboard
    [HttpGet("/Judge/Dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var judges = await _context.Judges
            .Include(j => j.Competition)
            .Include(j => j.Round)
            .Include(j => j.JudgeAssignments)
                .ThenInclude(ja => ja.Submission)
                    .ThenInclude(s => s.Team)
            .Include(j => j.JudgeAssignments)
                .ThenInclude(ja => ja.Submission)
                    .ThenInclude(s => s.Registration)
                        .ThenInclude(r => r!.User)
            .Where(j => j.UserId == userId && j.Status == "Active")
            .AsSplitQuery()
            .ToListAsync();

        var judgeIds = judges.Select(j => j.JudgeId).ToList();
        var submissionIds = judges.SelectMany(j => j.JudgeAssignments)
            .Select(ja => ja.SubmissionId).Distinct().ToList();

        var scoredSubmissionIds = await _context.Scores
            .Where(s => judgeIds.Contains(s.JudgeId) && submissionIds.Contains(s.SubmissionId))
            .Select(s => new { s.JudgeId, s.SubmissionId })
            .Distinct()
            .ToListAsync();

        var now = DateTime.UtcNow;

        var groups = judges.Select(j => new JudgeCompetitionGroupDto
        {
            CompetitionId = j.CompetitionId,
            CompetitionName = j.Competition.CompetitionName,
            JudgeRole = j.JudgeRole,
            RoundName = j.Round?.RoundName,
            TotalAssigned = j.JudgeAssignments.Count,
            Completed = j.JudgeAssignments.Count(ja => ja.Status == "Completed"),
            Assignments = j.JudgeAssignments.Select(ja => new JudgeAssignmentRowDto
            {
                AssignmentId = ja.AssignmentId,
                SubmissionId = ja.SubmissionId,
                SubmissionTitle = ja.Submission.Title,
                TeamOrRep = ja.Submission.Team?.TeamName
                            ?? ja.Submission.Registration?.User?.FullName
                            ?? "N/A",
                Status = ja.Status,
                GradingDeadline = ja.GradingDeadline,
                IsOverdue = ja.GradingDeadline.HasValue
                            && now > ja.GradingDeadline.Value
                            && ja.Status != "Completed",
                AlreadyScored = scoredSubmissionIds.Any(s => s.JudgeId == j.JudgeId && s.SubmissionId == ja.SubmissionId)
            }).OrderBy(a => a.Status == "Completed").ThenBy(a => a.GradingDeadline).ToList()
        }).ToList();

        var vm = new JudgeDashboardViewModel
        {
            TotalAssigned   = groups.Sum(g => g.TotalAssigned),
            TotalCompleted  = groups.Sum(g => g.Completed),
            TotalPending    = groups.Sum(g => g.Assignments.Count(a => a.Status == "Pending")),
            TotalInProgress = groups.Sum(g => g.Assignments.Count(a => a.Status == "InProgress")),
            CompetitionGroups = groups
        };

        return View("~/Views/Judge/Dashboard.cshtml", vm);
    }

    // GET /Judge/Grade/{assignmentId}
    [HttpGet("/Judge/Grade/{assignmentId:int}")]
    public async Task<IActionResult> Grade(int assignmentId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var assignment = await _context.JudgeAssignments
            .Include(ja => ja.Judge)
            .Include(ja => ja.Submission)
                .ThenInclude(s => s.Team)
                    .ThenInclude(t => t!.TeamMembers)
                        .ThenInclude(tm => tm.User)
            .Include(ja => ja.Submission)
                .ThenInclude(s => s.Registration)
                    .ThenInclude(r => r!.User)
            .Include(ja => ja.Competition)
            .Include(ja => ja.Round)
                .ThenInclude(r => r!.ScoringCriteria)
            .AsSplitQuery()
            .FirstOrDefaultAsync(ja => ja.AssignmentId == assignmentId && ja.Judge.UserId == userId);

        if (assignment == null)
            return NotFound("Không tìm thấy bài được phân công hoặc bạn không có quyền truy cập.");

        var existingScores = await _context.Scores
            .Where(s => s.SubmissionId == assignment.SubmissionId && s.JudgeId == assignment.JudgeId)
            .ToListAsync();

        var criteria = (assignment.Round?.ScoringCriteria ?? Enumerable.Empty<ScoringCriteria>())
            .OrderBy(c => c.Order)
            .Select(c => new CriteriaScoreInputDto
            {
                CriteriaId   = c.CriteriaId,
                CriteriaName = c.CriteriaName,
                Description  = c.Description,
                MaxScore     = c.MaxScore,
                Weight       = c.Weight,
                Order        = c.Order,
                Score   = existingScores.FirstOrDefault(s => s.CriteriaId == c.CriteriaId)?.Score,
                Comment = existingScores.FirstOrDefault(s => s.CriteriaId == c.CriteriaId)?.Comment
            }).ToList();

        var vm = new JudgeGradeViewModel
        {
            AssignmentId          = assignment.AssignmentId,
            AssignmentStatus      = assignment.Status,
            GradingDeadline       = assignment.GradingDeadline,
            SubmissionId          = assignment.SubmissionId,
            SubmissionTitle       = assignment.Submission.Title,
            SubmissionDescription = assignment.Submission.Description,
            FileUrl               = assignment.Submission.FileUrl,
            VideoUrl              = assignment.Submission.VideoUrl,
            ProjectLink           = assignment.Submission.ProjectLink,
            SubmissionStatus      = assignment.Submission.Status,
            TeamOrRep             = assignment.Submission.Team?.TeamName
                                    ?? assignment.Submission.Registration?.User?.FullName
                                    ?? "N/A",
            CompetitionId   = assignment.CompetitionId,
            CompetitionName = assignment.Competition.CompetitionName,
            RoundName       = assignment.Round?.RoundName,
            JudgeId         = assignment.JudgeId,
            AlreadyScored   = existingScores.Any(),
            Criteria        = criteria
        };

        return View("~/Views/Judge/Grade.cshtml", vm);
    }

    // POST /Judge/Grade/{assignmentId}
    [HttpPost("/Judge/Grade/{assignmentId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveGrade(int assignmentId, JudgeGradeViewModel model)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var assignment = await _context.JudgeAssignments
            .Include(ja => ja.Judge)
            .Include(ja => ja.Round)
                .ThenInclude(r => r!.ScoringCriteria)
            .FirstOrDefaultAsync(ja => ja.AssignmentId == assignmentId && ja.Judge.UserId == userId);

        if (assignment == null)
            return NotFound("Không tìm thấy bài được phân công.");

        if (assignment.Status == "Completed")
        {
            TempData["ErrorMessage"] = "Bài này đã được chấm điểm xong.";
            return RedirectToAction("Grade", new { assignmentId });
        }

        // Validate: tất cả criteria phải có điểm
        if (model.Criteria == null || model.Criteria.Any(c => !c.Score.HasValue))
        {
            TempData["ErrorMessage"] = "Vui lòng nhập điểm cho tất cả tiêu chí trước khi lưu.";
            return RedirectToAction("Grade", new { assignmentId });
        }

        var now = DateTime.UtcNow;
        var criteriaMap = (assignment.Round?.ScoringCriteria ?? Enumerable.Empty<ScoringCriteria>())
            .ToDictionary(c => c.CriteriaId);

        foreach (var input in model.Criteria)
        {
            if (!input.Score.HasValue) continue;

            var maxScore = criteriaMap.TryGetValue(input.CriteriaId, out var c) ? c.MaxScore : 10m;
            var clampedScore = Math.Clamp(input.Score.Value, 0, maxScore);

            var existing = await _context.Scores.FirstOrDefaultAsync(s =>
                s.SubmissionId == assignment.SubmissionId
                && s.JudgeId == assignment.JudgeId
                && s.CriteriaId == input.CriteriaId);

            if (existing != null)
            {
                existing.Score     = clampedScore;
                existing.Comment   = input.Comment?.Trim();
                existing.UpdatedAt = now;
            }
            else
            {
                _context.Scores.Add(new Scores
                {
                    SubmissionId = assignment.SubmissionId,
                    JudgeId      = assignment.JudgeId,
                    CriteriaId   = input.CriteriaId,
                    Score        = clampedScore,
                    Comment      = input.Comment?.Trim(),
                    ScoredDate   = now
                });
            }
        }

        assignment.Status      = "Completed";
        assignment.CompletedAt = now;
        assignment.UpdatedAt   = now;

        // Nếu submission chưa có status "Under Review", cập nhật
        var submission = await _context.Submissions.FindAsync(assignment.SubmissionId);
        if (submission != null && submission.Status == "Submitted")
        {
            submission.Status    = "Under Review";
            submission.UpdatedAt = now;
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Chấm điểm thành công!";
        return RedirectToAction("Dashboard");
    }
}
