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
                .ThenInclude(ja => ja.Submission).ThenInclude(s => s.Team)
            .Include(j => j.JudgeAssignments)
                .ThenInclude(ja => ja.Submission).ThenInclude(s => s.Registration).ThenInclude(r => r!.User)
            .Where(j => j.UserId == userId && j.Status == "Active")
            .AsSplitQuery()
            .ToListAsync();

        var judgeIds = judges.Select(j => j.JudgeId).ToList();
        var submissionIds = judges.SelectMany(j => j.JudgeAssignments)
            .Select(ja => ja.SubmissionId).Distinct().ToList();

        var scoredPairs = await _context.Scores
            .Where(s => judgeIds.Contains(s.JudgeId) && submissionIds.Contains(s.SubmissionId))
            .Select(s => new { s.JudgeId, s.SubmissionId })
            .Distinct()
            .ToListAsync();

        var now = DateTime.UtcNow;

        var groups = judges.Select(j => new JudgeCompetitionGroupDto
        {
            CompetitionId   = j.CompetitionId,
            CompetitionName = j.Competition.CompetitionName,
            JudgeRole       = j.JudgeRole,
            RoundName       = j.Round?.RoundName,
            TotalAssigned   = j.JudgeAssignments.Count,
            Completed       = j.JudgeAssignments.Count(ja => ja.Status == "Completed"),
            Assignments = j.JudgeAssignments.Select(ja => new JudgeAssignmentRowDto
            {
                AssignmentId    = ja.AssignmentId,
                SubmissionId    = ja.SubmissionId,
                SubmissionTitle = ja.Submission.Title,
                TeamOrRep       = ja.Submission.Team?.TeamName
                                  ?? ja.Submission.Registration?.User?.FullName
                                  ?? "N/A",
                Status          = ja.Status,
                GradingDeadline = ja.GradingDeadline,
                IsOverdue       = ja.GradingDeadline.HasValue && now > ja.GradingDeadline.Value && ja.Status != "Completed",
                AlreadyScored   = scoredPairs.Any(s => s.JudgeId == j.JudgeId && s.SubmissionId == ja.SubmissionId)
            }).OrderBy(a => a.Status == "Completed").ThenBy(a => a.GradingDeadline).ToList()
        }).ToList();

        var vm = new JudgeDashboardViewModel
        {
            TotalAssigned     = groups.Sum(g => g.TotalAssigned),
            TotalCompleted    = groups.Sum(g => g.Completed),
            TotalPending      = groups.Sum(g => g.Assignments.Count(a => a.Status == "Pending")),
            TotalInProgress   = groups.Sum(g => g.Assignments.Count(a => a.Status == "InProgress")),
            CompetitionGroups = groups
        };

        // HeadJudge: đếm số điểm chờ duyệt
        var hjCompetitionRoundPairs = judges
            .Where(j => j.JudgeRole == "HeadJudge")
            .Select(j => new { j.CompetitionId, j.RoundId })
            .ToList();

        ViewBag.IsHeadJudge = hjCompetitionRoundPairs.Any();

        if (hjCompetitionRoundPairs.Any())
        {
            var compIds  = hjCompetitionRoundPairs.Select(p => p.CompetitionId).Distinct().ToList();
            var roundIds = hjCompetitionRoundPairs.Select(p => p.RoundId).Where(r => r.HasValue).Select(r => r!.Value).Distinct().ToList();

            var otherJudgeIds = await _context.Judges
                .Where(j => compIds.Contains(j.CompetitionId)
                         && (roundIds.Count == 0 || (j.RoundId.HasValue && roundIds.Contains(j.RoundId.Value)))
                         && j.UserId != userId
                         && j.Status == "Active")
                .Select(j => j.JudgeId)
                .ToListAsync();

            ViewBag.PendingApprovalCount = otherJudgeIds.Any()
                ? await _context.Scores.CountAsync(s => otherJudgeIds.Contains(s.JudgeId) && s.ApprovalStatus == "Pending")
                : 0;
        }
        else
        {
            ViewBag.PendingApprovalCount = 0;
        }

        return View("~/Views/Judge/Dashboard.cshtml", vm);
    }

    // GET /Judge/Grade/{assignmentId}
    [HttpGet("/Judge/Grade/{assignmentId:int}")]
    public async Task<IActionResult> Grade(int assignmentId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var assignment = await _context.JudgeAssignments
            .Include(ja => ja.Judge)
            .Include(ja => ja.Submission).ThenInclude(s => s.Team).ThenInclude(t => t!.TeamMembers).ThenInclude(tm => tm.User)
            .Include(ja => ja.Submission).ThenInclude(s => s.Registration).ThenInclude(r => r!.User)
            .Include(ja => ja.Competition)
            .Include(ja => ja.Round).ThenInclude(r => r!.ScoringCriteria)
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
                                    ?? assignment.Submission.Registration?.User?.FullName ?? "N/A",
            CompetitionId   = assignment.CompetitionId,
            CompetitionName = assignment.Competition.CompetitionName,
            RoundName       = assignment.Round?.RoundName,
            JudgeId         = assignment.JudgeId,
            AlreadyScored   = existingScores.Any(),
            IsRejected      = existingScores.Any(s => s.ApprovalStatus == "Rejected"),
            RejectionReason = existingScores.FirstOrDefault(s => s.ApprovalStatus == "Rejected")?.RejectionReason,
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
            .Include(ja => ja.Round).ThenInclude(r => r!.ScoringCriteria)
            .FirstOrDefaultAsync(ja => ja.AssignmentId == assignmentId && ja.Judge.UserId == userId);

        if (assignment == null) return NotFound("Không tìm thấy bài được phân công.");

        if (assignment.Status == "Completed")
        {
            TempData["ErrorMessage"] = "Bài này đã được chấm điểm xong.";
            return RedirectToAction("Grade", new { assignmentId });
        }

        if (model.Criteria == null || model.Criteria.Any(c => !c.Score.HasValue))
        {
            TempData["ErrorMessage"] = "Vui lòng nhập điểm cho tất cả tiêu chí trước khi lưu.";
            return RedirectToAction("Grade", new { assignmentId });
        }

        var now = DateTime.UtcNow;
        var criteriaMap = (assignment.Round?.ScoringCriteria ?? Enumerable.Empty<ScoringCriteria>())
            .ToDictionary(c => c.CriteriaId);

        // Kiểm tra giám khảo này có phải trưởng ban không
        var isHeadJudge = assignment.Judge.JudgeRole == "HeadJudge";

        foreach (var input in model.Criteria)
        {
            if (!input.Score.HasValue) continue;
            var maxScore     = criteriaMap.TryGetValue(input.CriteriaId, out var c) ? c.MaxScore : 10m;
            var clampedScore = Math.Clamp(input.Score.Value, 0, maxScore);

            var existing = await _context.Scores.FirstOrDefaultAsync(s =>
                s.SubmissionId == assignment.SubmissionId
                && s.JudgeId   == assignment.JudgeId
                && s.CriteriaId == input.CriteriaId);

            if (existing != null)
            {
                existing.Score     = clampedScore;
                existing.Comment   = input.Comment?.Trim();
                existing.UpdatedAt = now;
                if (isHeadJudge)
                {
                    existing.ApprovalStatus  = "Approved";
                    existing.ApprovedBy      = userId;
                    existing.ApprovedAt      = now;
                    existing.RejectionReason = null;
                }
                else
                {
                    // Reset về Pending để trưởng ban duyệt lại sau khi giám khảo chấm lại
                    existing.ApprovalStatus  = "Pending";
                    existing.RejectionReason = null;
                    existing.ApprovedBy      = null;
                    existing.ApprovedAt      = null;
                }
            }
            else
            {
                _context.Scores.Add(new Scores
                {
                    SubmissionId    = assignment.SubmissionId,
                    JudgeId         = assignment.JudgeId,
                    CriteriaId      = input.CriteriaId,
                    Score           = clampedScore,
                    Comment         = input.Comment?.Trim(),
                    ScoredDate      = now,
                    ApprovalStatus  = isHeadJudge ? "Approved" : "Pending",
                    ApprovedBy      = isHeadJudge ? userId : null,
                    ApprovedAt      = isHeadJudge ? now : null
                });
            }
        }

        assignment.Status      = "Completed";
        assignment.CompletedAt = now;
        assignment.UpdatedAt   = now;

        var submission = await _context.Submissions.FindAsync(assignment.SubmissionId);
        if (submission != null && submission.Status == "Submitted")
        {
            submission.Status    = "Under Review";
            submission.UpdatedAt = now;
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = isHeadJudge
            ? "Chấm điểm thành công! Điểm của bạn đã được tự động duyệt."
            : "Chấm điểm thành công! Đang chờ trưởng ban duyệt điểm.";
        return RedirectToAction("Dashboard");
    }

    // GET /Judge/Review
    [HttpGet("/Judge/Review")]
    public async Task<IActionResult> Review()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var headJudgeRecords = await _context.Judges
            .Include(j => j.Competition)
            .Include(j => j.Round)
            .Where(j => j.UserId == userId && j.JudgeRole == "HeadJudge" && j.Status == "Active")
            .ToListAsync();

        var vm = new JudgeReviewViewModel();
        if (!headJudgeRecords.Any())
            return View("~/Views/Judge/Review.cshtml", vm);

        foreach (var hj in headJudgeRecords)
        {
            // Tất cả giám khảo trong vòng thi này
            var judgesInRound = await _context.Judges
                .Include(j => j.User)
                .Where(j => j.CompetitionId == hj.CompetitionId
                         && j.RoundId == hj.RoundId
                         && j.Status == "Active")
                .ToListAsync();

            // Tất cả assignments trong vòng thi
            var assignments = await _context.JudgeAssignments
                .Include(ja => ja.Submission).ThenInclude(s => s.Team)
                .Include(ja => ja.Submission).ThenInclude(s => s.Registration).ThenInclude(r => r!.User)
                .Where(ja => ja.CompetitionId == hj.CompetitionId && ja.RoundId == hj.RoundId)
                .AsSplitQuery()
                .ToListAsync();

            var subIds  = assignments.Select(a => a.SubmissionId).Distinct().ToList();
            var jIds    = judgesInRound.Select(j => j.JudgeId).ToList();

            var allScores = await _context.Scores
                .Include(s => s.Criteria)
                .Where(s => subIds.Contains(s.SubmissionId) && jIds.Contains(s.JudgeId))
                .ToListAsync();

            var submissionDtos = subIds.Select(subId =>
            {
                var sample   = assignments.First(a => a.SubmissionId == subId);
                var teamOrRep = sample.Submission.Team?.TeamName
                    ?? sample.Submission.Registration?.User?.FullName ?? "N/A";

                var judgeGroups = judgesInRound.Select(j =>
                {
                    var judgeScores = allScores
                        .Where(s => s.JudgeId == j.JudgeId && s.SubmissionId == subId)
                        .ToList();

                    string overallStatus = "NotScored";
                    if (judgeScores.Any())
                    {
                        overallStatus = judgeScores.All(s => s.ApprovalStatus == "Approved") ? "Approved"
                            : judgeScores.Any(s => s.ApprovalStatus == "Rejected") ? "Rejected"
                            : "Pending";
                    }

                    return new JudgeScoreGroupDto
                    {
                        JudgeId              = j.JudgeId,
                        UserId               = j.UserId,
                        JudgeName            = j.User.FullName,
                        JudgeRole            = j.JudgeRole,
                        IsCurrentHeadJudge   = j.UserId == userId,
                        OverallApprovalStatus = overallStatus,
                        RejectionReason      = judgeScores.FirstOrDefault(s => !string.IsNullOrEmpty(s.RejectionReason))?.RejectionReason,
                        Scores = judgeScores.Select(s => new ScoreDetailDto
                        {
                            ScoreId      = s.ScoreId,
                            CriteriaName = s.Criteria.CriteriaName,
                            MaxScore     = s.Criteria.MaxScore,
                            Weight       = s.Criteria.Weight,
                            Score        = s.Score,
                            Comment      = s.Comment
                        }).ToList()
                    };
                }).ToList();

                return new SubmissionApprovalDto
                {
                    SubmissionId    = subId,
                    Title           = sample.Submission.Title,
                    TeamOrRep       = teamOrRep,
                    AllApproved     = judgeGroups.All(g => g.OverallApprovalStatus == "Approved"),
                    JudgeScoreGroups = judgeGroups
                };
            }).ToList();

            int pendingCount  = submissionDtos.Sum(s => s.JudgeScoreGroups.Count(g => !g.IsCurrentHeadJudge && g.OverallApprovalStatus == "Pending"));
            int approvedCount = submissionDtos.Sum(s => s.JudgeScoreGroups.Count(g => !g.IsCurrentHeadJudge && g.OverallApprovalStatus == "Approved"));
            int totalGroups   = submissionDtos.Sum(s => s.JudgeScoreGroups.Count(g => !g.IsCurrentHeadJudge));

            vm.Rounds.Add(new RoundReviewDto
            {
                RoundId              = hj.RoundId ?? 0,
                RoundName            = hj.Round?.RoundName ?? "Không xác định",
                CompetitionName      = hj.Competition.CompetitionName,
                HeadJudgeId          = hj.JudgeId,
                PendingApprovalCount = pendingCount,
                ApprovedCount        = approvedCount,
                TotalJudgeScoreGroups = totalGroups,
                Submissions          = submissionDtos
            });
        }

        return View("~/Views/Judge/Review.cshtml", vm);
    }

    // POST /Judge/ApproveScores
    [HttpPost("/Judge/ApproveScores")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveScores([FromBody] ApproveScoresRequest dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var targetJudge = await _context.Judges
            .FirstOrDefaultAsync(j => j.JudgeId == dto.JudgeId && j.Status == "Active");

        if (targetJudge == null)
            return NotFound(new { message = "Không tìm thấy giám khảo." });

        // Không cho duyệt điểm của chính mình qua endpoint này
        if (targetJudge.UserId == userId)
            return BadRequest(new { message = "Bạn không cần duyệt điểm của chính mình." });

        // Xác nhận current user là HeadJudge trong cùng vòng thi
        var isHeadJudge = await _context.Judges.AnyAsync(j =>
            j.UserId        == userId
            && j.CompetitionId == targetJudge.CompetitionId
            && j.RoundId       == targetJudge.RoundId
            && j.JudgeRole     == "HeadJudge"
            && j.Status        == "Active");

        if (!isHeadJudge)
            return Forbid();

        var scores = await _context.Scores
            .Where(s => s.JudgeId == dto.JudgeId && s.SubmissionId == dto.SubmissionId)
            .ToListAsync();

        if (!scores.Any())
            return NotFound(new { message = "Giám khảo này chưa chấm bài nộp đó." });

        var now = DateTime.UtcNow;

        if (dto.Action == "Approve")
        {
            foreach (var score in scores)
            {
                score.ApprovalStatus = "Approved";
                score.ApprovedBy     = userId;
                score.ApprovedAt     = now;
                score.RejectionReason = null;
                score.UpdatedAt      = now;
            }
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã duyệt điểm thành công." });
        }

        if (dto.Action == "Reject")
        {
            if (string.IsNullOrWhiteSpace(dto.RejectionReason))
                return BadRequest(new { message = "Vui lòng nhập lý do từ chối." });

            foreach (var score in scores)
            {
                score.ApprovalStatus  = "Rejected";
                score.RejectionReason = dto.RejectionReason.Trim();
                score.UpdatedAt       = now;
            }

            // Reset assignment → giám khảo phải chấm lại
            var assignment = await _context.JudgeAssignments.FirstOrDefaultAsync(ja =>
                ja.JudgeId == dto.JudgeId && ja.SubmissionId == dto.SubmissionId);
            if (assignment != null)
            {
                assignment.Status      = "Pending";
                assignment.CompletedAt = null;
                assignment.UpdatedAt   = now;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã từ chối điểm. Giám khảo sẽ cần chấm lại." });
        }

        return BadRequest(new { message = "Hành động không hợp lệ." });
    }
}
