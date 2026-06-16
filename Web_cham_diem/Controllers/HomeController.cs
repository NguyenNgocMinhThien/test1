using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web_cham_diem.Models;
using Web_cham_diem.Models.ViewModels;

namespace Web_cham_diem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var now = DateTime.UtcNow;

            // ── Stats ──────────────────────────────────────────────────────────
            var activeCompCount = await _context.Competitions
                .CountAsync(c => c.Status == "Active");

            var totalParticipants = await _context.Registrations
                .Select(r => r.UserId).Distinct().CountAsync();

            var totalCategories = await _context.Competitions
                .Where(c => c.Category != null)
                .Select(c => c.Category!)
                .Distinct().CountAsync();

            var totalEvaluated = await _context.Submissions
                .CountAsync(s => s.Status == "Evaluated");

            // ── Featured competitions (Active, up to 3) ────────────────────────
            var activeComps = await _context.Competitions
                .Include(c => c.RegistrationRounds)
                .Include(c => c.Registrations)
                .Include(c => c.CompetitionImages)
                .Where(c => c.Status == "Active")
                .OrderByDescending(c => c.CreatedAt)
                .Take(3)
                .ToListAsync();

            var featuredComps = activeComps.Select(c =>
            {
                var nearestDeadline = c.RegistrationRounds
                    .Where(r => r.EndDate >= now)
                    .OrderBy(r => r.EndDate)
                    .FirstOrDefault();
                var daysLeft = nearestDeadline != null
                    ? Math.Max(0, (int)(nearestDeadline.EndDate - now).TotalDays) : -1;
                var bannerUrl = c.CompetitionImages.FirstOrDefault(i => i.IsThumbnail)?.ImageUrl
                             ?? c.CompetitionImages.FirstOrDefault()?.ImageUrl;
                return new HomeFeaturedCompetitionDto
                {
                    CompetitionId      = c.CompetitionId,
                    CompetitionName    = c.CompetitionName,
                    Category           = c.Category,
                    Description        = c.Description,
                    Prize              = c.Prize,
                    RegistrationDeadline = nearestDeadline?.EndDate,
                    DaysRemaining      = daysLeft,
                    MaxParticipants    = c.MaxParticipants,
                    IsTeamBased        = c.IsTeamBased,
                    MaxTeamSize        = c.MaxTeamSize,
                    BannerImageUrl     = bannerUrl,
                    TotalRegistrations = c.Registrations.Count
                };
            }).ToList();

            // ── Top results (best scorer per competition, top 3) ──────────────
            var evaluatedSubs = await _context.Submissions
                .Include(s => s.Registration).ThenInclude(r => r!.User)
                .Include(s => s.Team)
                .Include(s => s.Competition)
                .Where(s => s.Status == "Evaluated")
                .ToListAsync();

            var subIds = evaluatedSubs.Select(s => s.SubmissionId).ToList();
            var allScores = subIds.Count > 0
                ? await _context.Scores
                    .Where(s => subIds.Contains(s.SubmissionId) && s.ApprovalStatus != "Rejected")
                    .ToListAsync()
                : new List<Scores>();

            var compWinners = new List<(Submissions Sub, decimal Score)>();
            foreach (var group in evaluatedSubs.GroupBy(s => s.CompetitionId))
            {
                Submissions? topSub = null;
                decimal topScore = -1;
                foreach (var sub in group)
                {
                    var sc = allScores.Where(x => x.SubmissionId == sub.SubmissionId).ToList();
                    if (!sc.Any()) continue;
                    var total = sc.GroupBy(x => x.CriteriaId)
                                  .Sum(g => (decimal)g.Average(x => (double)x.Score));
                    if (total > topScore) { topScore = total; topSub = sub; }
                }
                if (topSub != null) compWinners.Add((topSub, topScore));
            }

            var topResults = compWinners
                .OrderByDescending(w => w.Score)
                .Take(3)
                .Select((w, i) => new HomeTopResultDto
                {
                    ParticipantName = w.Sub.Team?.TeamName
                        ?? w.Sub.Registration?.User?.FullName
                        ?? "Không xác định",
                    CompetitionName = w.Sub.Competition?.CompetitionName ?? "",
                    AwardLevel      = i == 0 ? "Nhất" : i == 1 ? "Nhì" : "Ba",
                    TotalScore      = Math.Round(w.Score, 1),
                    Rank            = i + 1
                })
                .ToList();

            // ── Latest notifications (distinct broadcasts, up to 4) ────────────
            var rawNotifs = await _context.Notifications
                .OrderByDescending(n => n.CreatedAt)
                .Take(200)
                .Select(n => new
                {
                    n.Title, n.Message, n.Type,
                    n.RelatedEntity, n.RelatedEntityId, n.CreatedAt
                })
                .ToListAsync();

            var latestNotifs = rawNotifs
                .GroupBy(n => (n.Title, n.RelatedEntity, n.RelatedEntityId))
                .Select(g => g.OrderByDescending(n => n.CreatedAt).First())
                .OrderByDescending(n => n.CreatedAt)
                .Take(4)
                .Select(n => new HomeNotificationDto
                {
                    Title           = n.Title,
                    Message         = n.Message,
                    Type            = n.Type,
                    CreatedAt       = n.CreatedAt,
                    CategoryLabel   = GetNotifCategoryLabel(n.RelatedEntity),
                    RelatedEntityId = n.RelatedEntityId
                })
                .ToList();

            var vm = new HomeViewModel
            {
                ActiveCompetitionsCount = activeCompCount,
                TotalParticipantsCount  = totalParticipants,
                TotalCategoriesCount    = totalCategories,
                TotalEvaluatedCount     = totalEvaluated,
                FeaturedCompetitions    = featuredComps,
                TopResults              = topResults,
                LatestNotifications     = latestNotifs
            };

            return View(vm);
        }

        private static string GetNotifCategoryLabel(string? relatedEntity) => relatedEntity switch
        {
            "Milestone_RegistrationOpen"    => "Mở đăng ký",
            "Milestone_ReviewResults"       => "Xét duyệt",
            "Milestone_SubmissionDeadline"  => "Hạn nộp bài",
            "Milestone_GradingSchedule"     => "Lịch chấm thi",
            "Milestone_ResultsAnnouncement" => "Công bố kết quả",
            _                               => "Thông báo chung"
        };

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
