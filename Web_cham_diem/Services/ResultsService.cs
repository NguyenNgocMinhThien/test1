using Microsoft.EntityFrameworkCore;
using Web_cham_diem.Models;
using Web_cham_diem.Models.ViewModels;

namespace Web_cham_diem.Services;

public class ResultsService : IResultsService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ResultsService> _logger;

    public ResultsService(ApplicationDbContext context, ILogger<ResultsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ResultsPageViewModel> GetResultsViewAsync(int organizerId, int? competitionId)
    {
        var vm = new ResultsPageViewModel();

        var allComps = await _context.Competitions
            .Where(c => c.CreatedByUserId == organizerId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        var ownedIds = allComps.Select(c => c.CompetitionId).ToList();

        vm.Competitions = allComps.Select(c => new CompetitionSelectorDto
        {
            CompetitionId = c.CompetitionId,
            CompetitionName = c.CompetitionName,
            Status = c.Status
        }).ToList();

        // Overall aggregate stats (chỉ trong phạm vi cuộc thi của organizer)
        // Lưu ý: Submissions.Status không bao giờ được cập nhật thành "Evaluated" trong luồng chấm điểm
        // thực tế (JudgeController chỉ chuyển "Submitted" -> "Under Review"), nên "đã chấm" phải được
        // xác định qua sự tồn tại của điểm số (Scores) đã được duyệt/không bị từ chối, giống cách
        // BuildRankings xác định bài nào có điểm để xếp hạng.
        vm.TotalCompetitions = allComps.Count;
        vm.TotalRegistrations = await _context.Registrations.CountAsync(r => ownedIds.Contains(r.CompetitionId));
        vm.TotalSubmissions = await _context.Submissions.CountAsync(s => ownedIds.Contains(s.CompetitionId));
        vm.TotalEvaluatedSubmissions = await _context.Submissions
            .CountAsync(s => ownedIds.Contains(s.CompetitionId) && s.Scores.Any(sc => sc.ApprovalStatus != "Rejected"));
        vm.TotalRankedRounds = await _context.CompetitionRounds.CountAsync(r => ownedIds.Contains(r.CompetitionId) && r.Status == "Completed");

        // Per-competition stats for summary tab
        var regCounts = await _context.Registrations
            .Where(r => ownedIds.Contains(r.CompetitionId))
            .GroupBy(r => r.CompetitionId)
            .Select(g => new { CompId = g.Key, Count = g.Count() })
            .ToListAsync();

        var subCounts = await _context.Submissions
            .Where(s => ownedIds.Contains(s.CompetitionId))
            .GroupBy(s => s.CompetitionId)
            .Select(g => new
            {
                CompId = g.Key,
                Total = g.Count(),
                Evaluated = g.Count(s => s.Scores.Any(sc => sc.ApprovalStatus != "Rejected"))
            })
            .ToListAsync();

        var roundCounts = await _context.CompetitionRounds
            .GroupBy(r => r.CompetitionId)
            .Select(g => new { CompId = g.Key, Total = g.Count(), Completed = g.Count(r => r.Status == "Completed") })
            .ToListAsync();

        vm.CompetitionSummaries = allComps.Select(c =>
        {
            var regs = regCounts.FirstOrDefault(x => x.CompId == c.CompetitionId);
            var subs = subCounts.FirstOrDefault(x => x.CompId == c.CompetitionId);
            var rds = roundCounts.FirstOrDefault(x => x.CompId == c.CompetitionId);
            int total = subs?.Total ?? 0;
            int eval = subs?.Evaluated ?? 0;
            return new CompetitionResultSummaryDto
            {
                CompetitionId = c.CompetitionId,
                CompetitionName = c.CompetitionName,
                Category = c.Category,
                Status = c.Status,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                TotalRegistrations = regs?.Count ?? 0,
                TotalSubmissions = total,
                EvaluatedSubmissions = eval,
                TotalRounds = rds?.Total ?? 0,
                CompletedRounds = rds?.Completed ?? 0,
                GradingCompletionRate = total > 0 ? Math.Round(eval * 100.0 / total, 1) : 0
            };
        }).ToList();

        // Default to first competition if none selected
        if (!competitionId.HasValue && allComps.Any())
            competitionId = allComps.First().CompetitionId;
        vm.SelectedCompetitionId = competitionId;

        if (!competitionId.HasValue) return vm;

        var comp = allComps.FirstOrDefault(c => c.CompetitionId == competitionId);
        if (comp == null) return vm;

        vm.SelectedDetail = await BuildDetailAsync(comp);

        // Patch summary with computed award data
        var summary = vm.CompetitionSummaries.FirstOrDefault(s => s.CompetitionId == competitionId);
        if (summary != null)
        {
            summary.TotalAwardedEntries = vm.SelectedDetail.TotalAwardedEntries;
            summary.AverageScore = vm.SelectedDetail.AverageScore;
        }

        return vm;
    }

    public async Task<(bool ok, string msg)> SetRoundResultsPublishedAsync(int roundId, int competitionId, bool publish)
    {
        var round = await _context.CompetitionRounds
            .FirstOrDefaultAsync(r => r.RoundId == roundId && r.CompetitionId == competitionId);
        if (round == null) return (false, "Vòng thi không tìm thấy.");

        round.IsResultsPublished = publish;
        round.ResultsPublishedAt = publish ? DateTime.UtcNow : null;
        round.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return (true, publish
            ? $"Đã công bố kết quả vòng '{round.RoundName}' ra trang công khai."
            : $"Đã ẩn kết quả vòng '{round.RoundName}' khỏi trang công khai.");
    }

    public async Task<OrganizerStatisticsViewModel> GetOrganizerStatisticsAsync(int organizerId, int? competitionId = null)
    {
        var allComps = await _context.Competitions
            .Where(c => c.CreatedByUserId == organizerId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        var vm = new OrganizerStatisticsViewModel
        {
            SelectedCompetitionId = competitionId,
            Competitions = allComps.Select(c => new CompetitionSelectorDto
            {
                CompetitionId = c.CompetitionId,
                CompetitionName = c.CompetitionName,
                Status = c.Status
            }).ToList(),
            TotalCompetitions = allComps.Count,
            ActiveCompetitions = allComps.Count(c => c.Status == "Active"),
            CompletedCompetitions = allComps.Count(c => c.Status == "Completed"),
            DraftCompetitions = allComps.Count(c => c.Status == "Draft")
        };

        var targetComps = competitionId.HasValue
            ? allComps.Where(c => c.CompetitionId == competitionId.Value).ToList()
            : allComps;

        foreach (var comp in targetComps)
        {
            var detail = await BuildDetailAsync(comp);

            vm.TotalRegistrations += detail.TotalRegistrations;
            vm.ApprovedRegistrations += detail.ApprovedRegistrations;
            vm.TotalSubmissions += detail.TotalSubmissions;
            vm.EvaluatedSubmissions += detail.EvaluatedSubmissions;
            vm.TotalAwardedEntries += detail.TotalAwardedEntries;

            foreach (var round in detail.Rounds)
            {
                vm.RoundResults.Add(new StatCompetitionRoundRow
                {
                    CompetitionId = comp.CompetitionId,
                    CompetitionName = comp.CompetitionName,
                    CompetitionStatus = comp.Status,
                    RoundName = round.RoundName,
                    RoundOrder = round.RoundOrder,
                    IsResultsPublished = round.IsResultsPublished,
                    SubmissionCount = round.SubmissionCount,
                    EvaluatedCount = round.EvaluatedCount,
                    GradingProgress = round.SubmissionCount > 0
                        ? Math.Round(round.EvaluatedCount * 100.0 / round.SubmissionCount, 1)
                        : 0,
                    AverageScore = round.AverageScore,
                    MaxPossibleScore = round.MaxPossibleScore,
                    AwardedCount = round.Rankings.Count(r => !string.IsNullOrEmpty(r.AwardLevel))
                });

                foreach (var r in round.Rankings.Where(r => !string.IsNullOrEmpty(r.AwardLevel)))
                {
                    vm.AwardWinners.Add(new StatAwardRow
                    {
                        CompetitionId = comp.CompetitionId,
                        CompetitionName = comp.CompetitionName,
                        RoundName = round.RoundName,
                        Rank = r.Rank,
                        AwardLevel = r.AwardLevel,
                        ParticipantName = r.ParticipantName,
                        StudentId = r.StudentId,
                        IsTeam = r.IsTeam,
                        Title = r.Title,
                        TotalScore = r.TotalScore,
                        ScorePercentage = r.ScorePercentage
                    });
                }
            }
        }

        vm.GradingCompletionRate = vm.TotalSubmissions > 0
            ? Math.Round((decimal)vm.EvaluatedSubmissions / vm.TotalSubmissions * 100, 1)
            : 0;

        vm.RoundResults = vm.RoundResults
            .OrderBy(r => r.CompetitionName)
            .ThenBy(r => r.RoundOrder)
            .ToList();

        vm.AwardWinners = vm.AwardWinners
            .OrderBy(a => a.CompetitionName)
            .ThenBy(a => a.RoundName)
            .ThenBy(a => a.Rank)
            .ToList();

        return vm;
    }

    private async Task<CompetitionResultDetailDto> BuildDetailAsync(Competitions comp)
    {
        int cid = comp.CompetitionId;

        var submissions = await _context.Submissions
            .Include(s => s.Registration).ThenInclude(r => r!.User)
            .Include(s => s.Team)
            .Where(s => s.CompetitionId == cid)
            .ToListAsync();

        var submissionIds = submissions.Select(s => s.SubmissionId).ToList();

        var allScores = submissionIds.Any()
            ? await _context.Scores
                .Where(s => submissionIds.Contains(s.SubmissionId) && s.ApprovalStatus != "Rejected")
                .ToListAsync()
            : new List<Scores>();

        // Bài được xem là "đã chấm" khi có ít nhất 1 điểm hợp lệ (không bị từ chối)
        var evaluatedSubmissionIds = allScores.Select(s => s.SubmissionId).ToHashSet();

        var rounds = await _context.CompetitionRounds
            .Include(r => r.ScoringCriteria)
            .Where(r => r.CompetitionId == cid)
            .OrderBy(r => r.RoundOrder)
            .ToListAsync();

        int judgesCount = await _context.Judges.CountAsync(j => j.CompetitionId == cid);
        int regCount = await _context.Registrations.CountAsync(r => r.CompetitionId == cid);
        int approvedRegCount = await _context.Registrations.CountAsync(r => r.CompetitionId == cid && r.Status == "Approved");

        var detail = new CompetitionResultDetailDto
        {
            CompetitionId = comp.CompetitionId,
            CompetitionName = comp.CompetitionName,
            Category = comp.Category,
            Status = comp.Status,
            StartDate = comp.StartDate,
            EndDate = comp.EndDate,
            Prize = comp.Prize,
            TotalRegistrations = regCount,
            ApprovedRegistrations = approvedRegCount,
            TotalSubmissions = submissions.Count,
            EvaluatedSubmissions = submissions.Count(s => evaluatedSubmissionIds.Contains(s.SubmissionId)),
            TotalJudges = judgesCount
        };

        var allPercentages = new List<decimal>();

        if (rounds.Any())
        {
            bool hasRoundLinks = submissions.Any(s => s.CompetitionRoundId.HasValue);

            foreach (var round in rounds)
            {
                var roundSubs = hasRoundLinks
                    ? submissions.Where(s => s.CompetitionRoundId == round.RoundId).ToList()
                    : submissions;

                var roundSubIds = roundSubs.Select(s => s.SubmissionId).ToHashSet();
                var roundScores = allScores.Where(s => roundSubIds.Contains(s.SubmissionId)).ToList();

                var criteria = round.ScoringCriteria.OrderBy(c => c.Order).ToList();
                decimal maxPossible = criteria.Any() ? criteria.Sum(c => c.MaxScore) : 100;

                var rankings = BuildRankings(roundSubs, roundScores, maxPossible);
                allPercentages.AddRange(rankings.Select(r => r.ScorePercentage));

                detail.Rounds.Add(new RoundResultDetailDto
                {
                    RoundId = round.RoundId,
                    RoundName = round.RoundName,
                    RoundOrder = round.RoundOrder,
                    Status = round.Status,
                    IsResultsPublished = round.IsResultsPublished,
                    ResultsPublishedAt = round.ResultsPublishedAt,
                    StartDate = round.StartDate,
                    EndDate = round.EndDate,
                    SubmissionCount = roundSubs.Count,
                    EvaluatedCount = roundSubs.Count(s => evaluatedSubmissionIds.Contains(s.SubmissionId)),
                    AverageScore = rankings.Any() ? Math.Round(rankings.Average(r => r.TotalScore), 1) : 0,
                    MaxPossibleScore = maxPossible,
                    Criteria = criteria.Select(c => new ScoringCriteriaResultDto
                    {
                        CriteriaId = c.CriteriaId,
                        CriteriaName = c.CriteriaName,
                        MaxScore = c.MaxScore,
                        Weight = c.Weight
                    }).ToList(),
                    Rankings = rankings
                });
            }
        }
        else if (submissions.Any() && allScores.Any())
        {
            // No rounds defined — treat as single general round
            var rankings = BuildRankings(submissions, allScores, 100);
            allPercentages.AddRange(rankings.Select(r => r.ScorePercentage));

            detail.Rounds.Add(new RoundResultDetailDto
            {
                RoundId = 0,
                RoundName = "Tổng hợp",
                RoundOrder = 1,
                Status = comp.Status,
                IsResultsPublished = true,
                StartDate = comp.StartDate,
                EndDate = comp.EndDate,
                SubmissionCount = submissions.Count,
                EvaluatedCount = submissions.Count(s => evaluatedSubmissionIds.Contains(s.SubmissionId)),
                AverageScore = rankings.Any() ? Math.Round(rankings.Average(r => r.TotalScore), 1) : 0,
                MaxPossibleScore = 100,
                Criteria = new(),
                Rankings = rankings
            });
        }

        // Score distribution
        detail.ScoreBelow50 = allPercentages.Count(p => p < 50);
        detail.Score50To65 = allPercentages.Count(p => p >= 50 && p < 65);
        detail.Score65To80 = allPercentages.Count(p => p >= 65 && p < 80);
        detail.Score80To90 = allPercentages.Count(p => p >= 80 && p < 90);
        detail.ScoreAbove90 = allPercentages.Count(p => p >= 90);

        var allRankings = detail.Rounds.SelectMany(r => r.Rankings).ToList();
        detail.FirstPrizeCount = allRankings.Count(r => r.AwardLevel == "Nhất");
        detail.SecondPrizeCount = allRankings.Count(r => r.AwardLevel == "Nhì");
        detail.ThirdPrizeCount = allRankings.Count(r => r.AwardLevel == "Ba");
        detail.EncouragementPrizeCount = allRankings.Count(r => r.AwardLevel == "Khuyến khích");
        detail.NoAwardCount = allRankings.Count(r => string.IsNullOrEmpty(r.AwardLevel));
        detail.TotalAwardedEntries = detail.FirstPrizeCount + detail.SecondPrizeCount
                                   + detail.ThirdPrizeCount + detail.EncouragementPrizeCount;

        detail.AverageScore = allRankings.Any() ? Math.Round(allRankings.Average(r => r.TotalScore), 1) : 0;
        detail.GradingCompletionRate = detail.TotalSubmissions > 0
            ? Math.Round(detail.EvaluatedSubmissions * 100.0 / detail.TotalSubmissions, 1)
            : 0;

        return detail;
    }

    private static List<SubmissionRankingDetailDto> BuildRankings(
        IEnumerable<Submissions> subs, IEnumerable<Scores> scores, decimal maxPossible)
    {
        var scoreList = scores.ToList();
        var rankings = new List<SubmissionRankingDetailDto>();

        foreach (var sub in subs)
        {
            var subScores = scoreList.Where(s => s.SubmissionId == sub.SubmissionId).ToList();
            if (!subScores.Any()) continue;

            var criteriaAvgs = subScores
                .GroupBy(s => s.CriteriaId)
                .ToDictionary(g => g.Key, g => Math.Round(g.Average(s => s.Score), 2));

            decimal total = criteriaAvgs.Values.Sum();
            decimal pct = maxPossible > 0 ? Math.Round(total / maxPossible * 100, 1) : 0;

            rankings.Add(new SubmissionRankingDetailDto
            {
                SubmissionId = sub.SubmissionId,
                Title = !string.IsNullOrEmpty(sub.Title) ? sub.Title : $"Bài #{sub.SubmissionId}",
                ParticipantName = sub.Team?.TeamName ?? sub.Registration?.User?.FullName ?? "Không xác định",
                StudentId = sub.Registration?.User?.StudentId,
                IsTeam = sub.TeamId.HasValue,
                TotalScore = total,
                MaxPossibleScore = maxPossible,
                ScorePercentage = pct,
                CriteriaScores = criteriaAvgs,
                JudgeCount = subScores.Select(s => s.JudgeId).Distinct().Count()
            });
        }

        rankings = rankings.OrderByDescending(r => r.TotalScore).ToList();
        int n = rankings.Count;
        for (int i = 0; i < n; i++)
        {
            rankings[i].Rank = i + 1;
            rankings[i].AwardLevel = ComputeAwardLevel(i + 1, n);
        }

        return rankings;
    }

    private static string ComputeAwardLevel(int rank, int total)
    {
        if (rank == 1) return "Nhất";
        if (rank == 2) return "Nhì";
        if (rank == 3) return "Ba";
        int encourageCutoff = Math.Max(6, (int)Math.Ceiling(total * 0.2));
        if (rank <= encourageCutoff) return "Khuyến khích";
        return "";
    }
}
