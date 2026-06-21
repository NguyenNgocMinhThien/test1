using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web_cham_diem.Models;
using Web_cham_diem.Models.ViewModels;
using Web_cham_diem.Services;

namespace Web_cham_diem.Controllers;

[Authorize(Roles = "Judge")]
public class JudgeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<JudgeController> _logger;
    private readonly INotificationsService _notifications;

    public JudgeController(ApplicationDbContext context, ILogger<JudgeController> logger, INotificationsService notifications)
    {
        _context = context;
        _logger = logger;
        _notifications = notifications;
    }

    private int GetCurrentUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // GET /Judge/Dashboard — Tổng quan / phân tích chấm điểm
    [HttpGet("/Judge/Dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var judges = await _context.Judges
            .Where(j => j.UserId == userId && j.Status == "Active")
            .ToListAsync();

        var judgeIds = judges.Select(j => j.JudgeId).ToList();

        var assignments = await _context.JudgeAssignments
            .Include(ja => ja.Submission)
            .Include(ja => ja.Competition)
            .Include(ja => ja.Round)
            .Where(ja => judgeIds.Contains(ja.JudgeId))
            .AsSplitQuery()
            .ToListAsync();

        var scores = await _context.Scores
            .Include(s => s.Criteria)
            .Where(s => judgeIds.Contains(s.JudgeId))
            .ToListAsync();

        var totalAssigned   = assignments.Count;
        var totalCompleted  = assignments.Count(a => a.Status == "Completed");
        var totalPending    = assignments.Count(a => a.Status == "Pending");
        var totalInProgress = assignments.Count(a => a.Status == "InProgress");
        var completionRate  = totalAssigned > 0
            ? Math.Round((double)totalCompleted / totalAssigned * 100, 1)
            : 0;

        var criteriaSummaries = scores
            .GroupBy(s => new { s.CriteriaId, s.Criteria.CriteriaName, s.Criteria.MaxScore })
            .Select(g => new CriteriaSummaryDto
            {
                CriteriaName = g.Key.CriteriaName,
                MaxScore     = g.Key.MaxScore,
                AverageScore = Math.Round(g.Average(s => s.Score), 2),
                ScoredCount  = g.Count(),
                CommentCount = g.Count(s => !string.IsNullOrWhiteSpace(s.Comment))
            })
            .OrderByDescending(c => c.ScoredCount)
            .ToList();

        var recentGradings = assignments
            .Where(a => a.Status == "Completed")
            .OrderByDescending(a => a.CompletedAt)
            .Take(6)
            .Select(a =>
            {
                var subScores = scores.Where(s => s.JudgeId == a.JudgeId && s.SubmissionId == a.SubmissionId).ToList();
                var total = subScores.Sum(s => s.Score * s.Criteria.Weight);
                var max   = subScores.Sum(s => s.Criteria.MaxScore * s.Criteria.Weight);
                var approvalStatus = !subScores.Any() ? "Pending"
                    : subScores.All(s => s.ApprovalStatus == "Approved") ? "Approved"
                    : subScores.Any(s => s.ApprovalStatus == "Rejected") ? "Rejected"
                    : "Pending";

                return new RecentGradingDto
                {
                    AssignmentId    = a.AssignmentId,
                    SubmissionTitle = a.Submission.Title,
                    CompetitionName = a.Competition.CompetitionName,
                    RoundName       = a.Round?.RoundName,
                    ApprovalStatus  = approvalStatus,
                    TotalScore      = Math.Round(total, 2),
                    MaxScore        = Math.Round(max, 2),
                    JudgeComment    = a.JudgeComment,
                    CompletedAt     = a.CompletedAt
                };
            }).ToList();

        var vm = new JudgeDashboardViewModel
        {
            TotalAssigned       = totalAssigned,
            TotalCompleted      = totalCompleted,
            TotalPending        = totalPending,
            TotalInProgress     = totalInProgress,
            CompletionRate      = completionRate,
            ApprovedScoresCount = scores.Count(s => s.ApprovalStatus == "Approved"),
            PendingScoresCount  = scores.Count(s => s.ApprovalStatus == "Pending"),
            RejectedScoresCount = scores.Count(s => s.ApprovalStatus == "Rejected"),
            CriteriaSummaries   = criteriaSummaries,
            RecentGradings      = recentGradings
        };

        await SetHeadJudgePendingApprovalAsync(userId, judges);

        return View("~/Views/Judge/Dashboard.cshtml", vm);
    }

    // GET /Judge/Assignments — Danh sách bài được phân công, theo từng cuộc thi
    [HttpGet("/Judge/Assignments")]
    public async Task<IActionResult> Assignments()
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
                TeamOrRep       = "",   // Ẩn danh hóa khi chấm điểm
                Status          = ja.Status,
                GradingDeadline = ja.GradingDeadline,
                IsOverdue       = ja.GradingDeadline.HasValue && now > ja.GradingDeadline.Value && ja.Status != "Completed",
                AlreadyScored   = scoredPairs.Any(s => s.JudgeId == j.JudgeId && s.SubmissionId == ja.SubmissionId)
            }).OrderBy(a => a.Status == "Completed").ThenBy(a => a.GradingDeadline).ToList()
        }).ToList();

        var vm = new JudgeAssignmentsViewModel
        {
            TotalAssigned     = groups.Sum(g => g.TotalAssigned),
            TotalCompleted    = groups.Sum(g => g.Completed),
            TotalPending      = groups.Sum(g => g.Assignments.Count(a => a.Status == "Pending")),
            TotalInProgress   = groups.Sum(g => g.Assignments.Count(a => a.Status == "InProgress")),
            CompetitionGroups = groups
        };

        await SetHeadJudgePendingApprovalAsync(userId, judges);

        return View("~/Views/Judge/Assignments.cshtml", vm);
    }

    // Tính số điểm chờ trưởng ban duyệt (hiển thị badge trên sidebar)
    private async Task SetHeadJudgePendingApprovalAsync(int userId, List<Judges> judges)
    {
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
            TeamOrRep             = "",   // Ẩn danh hóa khi chấm điểm
            CompetitionId   = assignment.CompetitionId,
            CompetitionName = assignment.Competition.CompetitionName,
            RoundName       = assignment.Round?.RoundName,
            JudgeId         = assignment.JudgeId,
            AlreadyScored   = existingScores.Any(),
            IsRejected      = existingScores.Any(s => s.ApprovalStatus == "Rejected"),
            RejectionReason = existingScores.FirstOrDefault(s => s.ApprovalStatus == "Rejected")?.RejectionReason,
            JudgeComment    = assignment.JudgeComment,
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

        assignment.Status       = "Completed";
        assignment.CompletedAt  = now;
        assignment.UpdatedAt    = now;
        assignment.JudgeComment = model.JudgeComment?.Trim();

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
        return RedirectToAction("Assignments");
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

    // GET /Judge/ScoreHistory
    [HttpGet("/Judge/ScoreHistory")]
    public async Task<IActionResult> ScoreHistory(
        [FromQuery] string? competition = null,
        [FromQuery] string? status      = null)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // Tất cả bản ghi giám khảo của user này
        var judges = await _context.Judges
            .Include(j => j.Competition)
            .Include(j => j.Round)
            .Where(j => j.UserId == userId && j.Status == "Active")
            .ToListAsync();

        var judgeIds = judges.Select(j => j.JudgeId).ToList();
        if (!judgeIds.Any())
        {
            return View("~/Views/Judge/ScoreHistory.cshtml", new JudgeScoreHistoryViewModel());
        }

        // Assignments (bao gồm cả Pending sau khi bị từ chối)
        var assignments = await _context.JudgeAssignments
            .Include(ja => ja.Submission)
            .Where(ja => judgeIds.Contains(ja.JudgeId))
            .ToListAsync();

        // Tất cả điểm đã chấm
        var allScores = await _context.Scores
            .Include(s => s.Criteria)
            .Where(s => judgeIds.Contains(s.JudgeId))
            .ToListAsync();

        var judgeMap = judges.ToDictionary(j => j.JudgeId);
        var now = DateTime.UtcNow;

        var items = new List<ScoreHistoryItemDto>();

        foreach (var ja in assignments)
        {
            var scores = allScores
                .Where(s => s.JudgeId == ja.JudgeId && s.SubmissionId == ja.SubmissionId)
                .ToList();

            if (!scores.Any()) continue;   // Chưa chấm → bỏ qua

            if (!judgeMap.TryGetValue(ja.JudgeId, out var judgeRecord)) continue;

            // Trạng thái duyệt tổng hợp
            string overallStatus;
            if (scores.All(s => s.ApprovalStatus == "Approved"))
                overallStatus = "Approved";
            else if (scores.Any(s => s.ApprovalStatus == "Rejected"))
                overallStatus = "Rejected";
            else
                overallStatus = "Pending";

            // Filter theo trạng thái
            if (!string.IsNullOrEmpty(status) && status != "all" && overallStatus != status)
                continue;

            // Filter theo cuộc thi
            if (!string.IsNullOrEmpty(competition) && judgeRecord.Competition.CompetitionName != competition)
                continue;

            // Tính điểm tổng
            decimal totalScore = 0, maxPossible = 0;
            foreach (var s in scores)
            {
                totalScore  += s.Score * s.Criteria.Weight;
                maxPossible += s.Criteria.MaxScore * s.Criteria.Weight;
            }

            var criteriaList = scores
                .OrderBy(s => s.Criteria.Order)
                .Select(s => new ScoreHistoryCriteriaDto
                {
                    CriteriaName   = s.Criteria.CriteriaName,
                    Score          = s.Score,
                    MaxScore       = s.Criteria.MaxScore,
                    Weight         = s.Criteria.Weight,
                    Comment        = s.Comment,
                    ApprovalStatus = s.ApprovalStatus
                }).ToList();

            items.Add(new ScoreHistoryItemDto
            {
                AssignmentId         = ja.AssignmentId,
                SubmissionId         = ja.SubmissionId,
                SubmissionTitle      = ja.Submission.Title,
                CompetitionName      = judgeRecord.Competition.CompetitionName,
                RoundName            = judgeRecord.Round?.RoundName,
                AssignmentStatus     = ja.Status,
                OverallApprovalStatus= overallStatus,
                RejectionReason      = scores.FirstOrDefault(s => !string.IsNullOrEmpty(s.RejectionReason))?.RejectionReason,
                ScoredDate           = scores.Min(s => (DateTime?)s.ScoredDate),
                LastUpdatedAt        = scores.Max(s => s.UpdatedAt),
                GradingDeadline      = ja.GradingDeadline,
                TotalScore           = Math.Round(totalScore, 2),
                MaxPossibleScore     = Math.Round(maxPossible, 2),
                // Có thể chỉnh sửa khi assignment bị từ chối (reset về Pending) và đã có điểm
                CanEdit              = ja.Status == "Pending" && scores.Any(),
                Criteria             = criteriaList
            });
        }

        // Sắp xếp: Rejected → Pending → Approved, rồi mới nhất trước
        items = items
            .OrderBy(i => i.OverallApprovalStatus == "Rejected" ? 0 : i.OverallApprovalStatus == "Pending" ? 1 : 2)
            .ThenByDescending(i => i.ScoredDate)
            .ToList();

        var availableCompetitions = judges
            .Select(j => j.Competition.CompetitionName)
            .Distinct()
            .OrderBy(n => n)
            .ToList();

        var vm = new JudgeScoreHistoryViewModel
        {
            TotalScored           = items.Count,
            ApprovedCount         = items.Count(i => i.OverallApprovalStatus == "Approved"),
            PendingCount          = items.Count(i => i.OverallApprovalStatus == "Pending"),
            RejectedCount         = items.Count(i => i.OverallApprovalStatus == "Rejected"),
            CompetitionFilter     = competition,
            StatusFilter          = status,
            AvailableCompetitions = availableCompetitions,
            Items                 = items
        };

        return View("~/Views/Judge/ScoreHistory.cshtml", vm);
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

    // GET /Judge/Notifications
    [HttpGet("/Judge/Notifications")]
    public async Task<IActionResult> Notifications(
        [FromQuery] string? type,
        [FromQuery] bool unreadOnly = false,
        [FromQuery] int page = 1)
    {
        var userId = GetCurrentUserId();
        var vm = await _notifications.GetUserNotificationsPageAsync(userId, type, unreadOnly, page, 15);
        return View(vm);
    }
}
