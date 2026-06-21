using Microsoft.EntityFrameworkCore;
using Web_cham_diem.Models;
using Web_cham_diem.Models.ViewModels;

namespace Web_cham_diem.Services;

public class NotificationsService : INotificationsService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NotificationsService> _logger;

    public NotificationsService(ApplicationDbContext context, ILogger<NotificationsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ───────── milestone template definitions ─────────

    private static readonly List<(string Key, string Name, string Description, string DefaultAudience, string NotifType, string Icon, string Color)> MilestoneTemplates =
    [
        ("RegistrationOpen",      "Mở đăng ký",          "Thông báo mở đăng ký tham gia cuộc thi",        "All",            "Info",    "bi-door-open",       "#10b981"),
        ("ReviewResults",         "Kết quả xét duyệt",   "Thông báo kết quả xét duyệt hồ sơ đăng ký",     "AllRegistrants", "Success", "bi-check-circle-fill","#3b82f6"),
        ("SubmissionDeadline",    "Hạn nộp bài",         "Nhắc nhở hạn chót nộp bài dự thi",              "Students",       "Warning", "bi-clock-fill",      "#f59e0b"),
        ("GradingSchedule",       "Lịch chấm thi",       "Thông báo lịch và địa điểm chấm thi",           "Judges",         "Info",    "bi-journal-check",   "#6366f1"),
        ("ResultsAnnouncement",   "Công bố kết quả",     "Thông báo công bố kết quả chính thức",          "All",            "Success", "bi-megaphone-fill",  "#8b5cf6"),
    ];

    // ───────── organizer page ─────────

    public async Task<OrganizerNotificationsViewModel> GetOrganizerViewAsync(int organizerId, int? competitionId)
    {
        var vm = new OrganizerNotificationsViewModel();

        var allComps = await _context.Competitions
            .Where(c => c.CreatedByUserId == organizerId)
            .Include(c => c.RegistrationRounds)
            .Include(c => c.CompetitionRounds)
            .Include(c => c.Registrations)
            .Include(c => c.Judges)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        var ownedIds = allComps.Select(c => c.CompetitionId).ToList();

        vm.Competitions = allComps.Select(c => new CompetitionBasicDto
        {
            CompetitionId = c.CompetitionId,
            CompetitionName = c.CompetitionName
        }).ToList();

        vm.TotalSentAll = await _context.Notifications.CountAsync(n => n.RelatedEntityId.HasValue && ownedIds.Contains(n.RelatedEntityId.Value));
        vm.UnreadByRecipients = await _context.Notifications.CountAsync(n => !n.IsRead && n.RelatedEntityId.HasValue && ownedIds.Contains(n.RelatedEntityId.Value));
        vm.ActiveCompetitionCount = allComps.Count(c => c.Status == "Active");

        if (!competitionId.HasValue && allComps.Count > 0)
            competitionId = allComps.First().CompetitionId;
        vm.SelectedCompetitionId = competitionId;

        // Upcoming milestones across all active/upcoming competitions (±30 days window)
        var now = DateTime.UtcNow;
        var upcomingAll = new List<UpcomingMilestoneItemDto>();

        foreach (var c in allComps.Where(c => c.Status != "Completed"))
        {
            var sentKeys = await _context.Notifications
                .Where(n => n.RelatedEntityId == c.CompetitionId
                         && n.RelatedEntity != null
                         && n.RelatedEntity.StartsWith("Milestone_"))
                .Select(n => n.RelatedEntity!)
                .Distinct()
                .ToListAsync();

            var dates = ComputeMilestoneDates(c);

            foreach (var t in MilestoneTemplates)
            {
                if (!dates.TryGetValue(t.Key, out var date) || date == null)
                    continue;

                int days = (int)Math.Round((date.Value - now).TotalDays, MidpointRounding.AwayFromZero);
                if (days < -3 || days > 30) continue;  // skip old or far future

                upcomingAll.Add(new UpcomingMilestoneItemDto
                {
                    Name = t.Name,
                    Date = date.Value,
                    CompetitionName = c.CompetitionName,
                    CompetitionId = c.CompetitionId,
                    MilestoneKey = t.Key,
                    IsOverdue = days < 0,
                    DaysUntil = days,
                    NotificationSent = sentKeys.Contains($"Milestone_{t.Key}"),
                    BadgeCss = days < 0 ? "bg-danger" : days <= 3 ? "bg-warning text-dark" : "bg-success",
                    IconClass = t.Icon
                });
            }
        }

        vm.UpcomingMilestones = [.. upcomingAll.OrderBy(m => m.DaysUntil)];
        vm.UpcomingMilestoneCount = upcomingAll.Count(m => !m.NotificationSent && m.DaysUntil >= 0 && m.DaysUntil <= 7);

        // Milestone tracker for selected competition
        if (competitionId.HasValue)
        {
            var comp = allComps.FirstOrDefault(c => c.CompetitionId == competitionId);
            if (comp != null)
                vm.MilestoneStatus = await BuildMilestoneStatusAsync(comp);
        }

        // Recent notification batches (grouped by title+entity to avoid per-user repetition)
        var baseQ = _context.Notifications.AsQueryable();
        if (competitionId.HasValue)
            baseQ = baseQ.Where(n => n.RelatedEntityId == competitionId);

        var rawRecent = await baseQ
            .OrderByDescending(n => n.CreatedAt)
            .Take(100)
            .Select(n => new { n.Title, n.Message, n.Type, n.RelatedEntity, n.RelatedEntityId, n.IsRead, n.CreatedAt })
            .ToListAsync();

        vm.RecentBatches = rawRecent
            .GroupBy(n => (n.Title, n.RelatedEntity, n.RelatedEntityId))
            .Select(g => new NotificationBatchDto
            {
                Title = g.Key.Title,
                Message = g.First().Message,
                Type = g.First().Type,
                MilestoneKey = g.Key.RelatedEntity?.Replace("Milestone_", ""),
                SentAt = g.Max(x => x.CreatedAt),
                RecipientCount = g.Count(),
                ReadCount = g.Count(x => x.IsRead),
                CategoryLabel = GetCategoryLabel(g.Key.RelatedEntity),
                CategoryCss = GetCategoryCss(g.First().Type)
            })
            .OrderByDescending(b => b.SentAt)
            .Take(15)
            .ToList();

        return vm;
    }

    // ───────── send notification ─────────

    public async Task<(bool Ok, string Message, int Count)> SendNotificationAsync(SendNotificationRequest req, int organizerId)
    {
        if (string.IsNullOrWhiteSpace(req.Title))
            return (false, "Tiêu đề không được để trống.", 0);

        // Xác nhận cuộc thi thuộc organizer này
        if (req.CompetitionId > 0)
        {
            var isOwner = await _context.Competitions
                .AnyAsync(c => c.CompetitionId == req.CompetitionId && c.CreatedByUserId == organizerId);
            if (!isOwner)
                return (false, "Bạn không có quyền gửi thông báo cho cuộc thi này.", 0);
        }

        var userIds = await ResolveAudienceAsync(req.CompetitionId, req.Audience);
        if (userIds.Count == 0)
            return (false, "Không tìm thấy người nhận phù hợp.", 0);

        var relatedEntity = string.IsNullOrWhiteSpace(req.MilestoneKey)
            ? "Competition"
            : $"Milestone_{req.MilestoneKey}";

        var now = DateTime.UtcNow;
        var records = userIds.Select(uid => new Notifications
        {
            UserId = uid,
            Title = req.Title.Trim(),
            Message = req.Message.Trim(),
            Type = req.Type,
            RelatedEntity = relatedEntity,
            RelatedEntityId = req.CompetitionId,
            IsRead = false,
            CreatedAt = now
        }).ToList();

        _context.Notifications.AddRange(records);
        await _context.SaveChangesAsync();

        return (true, $"Đã gửi thông báo tới {records.Count} người.", records.Count);
    }

    // ───────── user-facing helpers ─────────

    public async Task<int> GetUnreadCountAsync(int userId) =>
        await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

    public async Task<List<UserNotificationDto>> GetRecentForUserAsync(int userId, int take = 10)
    {
        var items = await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(take)
            .ToListAsync();

        return items.Select(MapToUserDto).ToList();
    }

    public async Task<UserNotificationsPageViewModel> GetUserNotificationsPageAsync(
        int userId, string? typeFilter, bool unreadOnly, int page, int pageSize)
    {
        // Load all user's notifications with competition name via left join
        var allRaw = await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new
            {
                n.NotificationId, n.Title, n.Message, n.Type,
                n.IsRead, n.CreatedAt, n.RelatedEntity, n.RelatedEntityId
            })
            .ToListAsync();

        // Load competition names for RelatedEntityIds
        var compIds = allRaw
            .Where(n => n.RelatedEntityId.HasValue)
            .Select(n => n.RelatedEntityId!.Value)
            .Distinct()
            .ToList();

        var compNames = await _context.Competitions
            .Where(c => compIds.Contains(c.CompetitionId))
            .Select(c => new { c.CompetitionId, c.CompetitionName })
            .ToDictionaryAsync(c => c.CompetitionId, c => c.CompetitionName);

        var vm = new UserNotificationsPageViewModel
        {
            TypeFilter  = typeFilter,
            UnreadOnly  = unreadOnly,
            TotalCount  = allRaw.Count,
            UnreadCount = allRaw.Count(n => !n.IsRead),
            InfoCount    = allRaw.Count(n => n.Type == "Info"),
            SuccessCount = allRaw.Count(n => n.Type == "Success"),
            WarningCount = allRaw.Count(n => n.Type == "Warning"),
            ErrorCount   = allRaw.Count(n => n.Type == "Error"),
        };

        // Apply filters in memory (already loaded)
        var filtered = allRaw.AsEnumerable();
        if (!string.IsNullOrEmpty(typeFilter))
            filtered = filtered.Where(n => n.Type == typeFilter);
        if (unreadOnly)
            filtered = filtered.Where(n => !n.IsRead);

        var filteredList = filtered.ToList();
        vm.TotalPages = Math.Max(1, (int)Math.Ceiling(filteredList.Count / (double)pageSize));
        vm.CurrentPage = Math.Clamp(page, 1, vm.TotalPages);
        vm.PageSize = pageSize;

        vm.Notifications = filteredList
            .Skip((vm.CurrentPage - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new UserNotificationDto
            {
                NotificationId = n.NotificationId,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
                RelatedEntity = n.RelatedEntity,
                RelatedEntityId = n.RelatedEntityId,
                IconClass = GetIconForType(n.Type),
                TimeAgo = FormatTimeAgo(n.CreatedAt),
                CategoryLabel = GetCategoryLabel(n.RelatedEntity),
                CompetitionName = n.RelatedEntityId.HasValue && compNames.TryGetValue(n.RelatedEntityId.Value, out var cn) ? cn : null
            })
            .ToList();

        return vm;
    }

    public async Task<bool> MarkReadAsync(int notificationId, int userId)
    {
        var notif = await _context.Notifications
            .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.UserId == userId);
        if (notif == null) return false;

        notif.IsRead = true;
        notif.ReadAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> MarkAllReadAsync(int userId)
    {
        var unread = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        var now = DateTime.UtcNow;
        foreach (var n in unread) { n.IsRead = true; n.ReadAt = now; }
        await _context.SaveChangesAsync();
        return unread.Count;
    }

    // ───────── private helpers ─────────

    private async Task<CompetitionMilestoneDto> BuildMilestoneStatusAsync(Competitions comp)
    {
        var sentGroups = await _context.Notifications
            .Where(n => n.RelatedEntityId == comp.CompetitionId
                     && n.RelatedEntity != null
                     && n.RelatedEntity.StartsWith("Milestone_"))
            .GroupBy(n => n.RelatedEntity!)
            .Select(g => new { Key = g.Key, SentAt = g.Min(n => n.CreatedAt), Count = g.Count() })
            .ToListAsync();

        var sentDict = sentGroups.ToDictionary(s => s.Key, s => (s.SentAt, s.Count));
        var dates = ComputeMilestoneDates(comp);

        var milestones = MilestoneTemplates.Select(t =>
        {
            var entityKey = $"Milestone_{t.Key}";
            var isSent = sentDict.TryGetValue(entityKey, out var info);
            dates.TryGetValue(t.Key, out var date);
            return new MilestoneItemDto
            {
                Key = t.Key,
                Name = t.Name,
                Description = t.Description,
                AudienceHint = t.DefaultAudience,
                Date = date,
                IsSent = isSent,
                SentAt = isSent ? info.SentAt : null,
                SentCount = isSent ? info.Count : 0,
                IconClass = t.Icon,
                ThemeColor = t.Color,
                NotifType = t.NotifType
            };
        }).ToList();

        return new CompetitionMilestoneDto
        {
            CompetitionId = comp.CompetitionId,
            CompetitionName = comp.CompetitionName,
            Status = comp.Status,
            TotalRegistrations = comp.Registrations.Count,
            ApprovedRegistrations = comp.Registrations.Count(r => r.Status == "Approved"),
            TotalJudges = comp.Judges.Count(j => j.Status == "Active"),
            Milestones = milestones
        };
    }

    private static Dictionary<string, DateTime?> ComputeMilestoneDates(Competitions comp)
    {
        var regOpen = comp.RegistrationRounds
            .OrderBy(r => r.StartDate).FirstOrDefault()?.StartDate ?? (DateTime?)comp.StartDate;
        var regClose = comp.RegistrationRounds
            .OrderByDescending(r => r.EndDate).FirstOrDefault()?.EndDate;
        var gradStart = comp.CompetitionRounds
            .OrderBy(r => r.RoundOrder).FirstOrDefault()?.StartDate;

        return new Dictionary<string, DateTime?>
        {
            ["RegistrationOpen"]    = regOpen,
            ["ReviewResults"]       = regClose,
            ["SubmissionDeadline"]  = comp.SubmissionDeadline,
            ["GradingSchedule"]     = gradStart,
            ["ResultsAnnouncement"] = comp.EndDate,
        };
    }

    private async Task<List<int>> ResolveAudienceAsync(int competitionId, string audience)
    {
        var ids = new HashSet<int>();

        if (audience == "All")
        {
            // Lấy danh sách userId của admin và của judge không thuộc cuộc thi này
            var adminIds = await _context.UserRoles
                .Where(ur => ur.RoleId == 1)
                .Select(ur => ur.UserId).ToListAsync();

            var judgesInComp = await _context.Judges
                .Where(j => j.CompetitionId == competitionId)
                .Select(j => j.UserId).Distinct().ToListAsync();

            var judgeRoleUserIds = await _context.UserRoles
                .Where(ur => ur.RoleId == 4)
                .Select(ur => ur.UserId).ToListAsync();

            // Giám khảo không tham gia cuộc thi này
            var excludedJudgeIds = judgeRoleUserIds.Except(judgesInComp).ToHashSet();

            var q = await _context.Users
                .Where(u => u.IsActive
                         && !adminIds.Contains(u.UserId)
                         && !excludedJudgeIds.Contains(u.UserId))
                .Select(u => u.UserId).Distinct().ToListAsync();
            foreach (var id in q) ids.Add(id);
        }
        else if (audience == "Students")
        {
            // Chỉ thí sinh/đội đã được duyệt trong cuộc thi
            var q = await _context.Registrations
                .Where(r => r.CompetitionId == competitionId && r.Status == "Approved")
                .Select(r => r.UserId).Distinct().ToListAsync();
            foreach (var id in q) ids.Add(id);
        }
        else if (audience == "AllRegistrants")
        {
            // Tất cả đăng ký cuộc thi bất kể trạng thái
            var q = await _context.Registrations
                .Where(r => r.CompetitionId == competitionId)
                .Select(r => r.UserId).Distinct().ToListAsync();
            foreach (var id in q) ids.Add(id);
        }
        else if (audience == "Judges")
        {
            // Tất cả giám khảo của cuộc thi
            var q = await _context.Judges
                .Where(j => j.CompetitionId == competitionId)
                .Select(j => j.UserId).Distinct().ToListAsync();
            foreach (var id in q) ids.Add(id);
        }

        return [.. ids];
    }

    private static string GetCategoryLabel(string? relatedEntity) => relatedEntity switch
    {
        "Milestone_RegistrationOpen"    => "Mở đăng ký",
        "Milestone_ReviewResults"       => "Xét duyệt",
        "Milestone_SubmissionDeadline"  => "Hạn nộp bài",
        "Milestone_GradingSchedule"     => "Lịch chấm thi",
        "Milestone_ResultsAnnouncement" => "Công bố kết quả",
        _                               => "Thông báo chung"
    };

    private static string GetCategoryCss(string type) => type switch
    {
        "Success" => "text-success",
        "Warning" => "text-warning",
        "Error"   => "text-danger",
        _         => "text-info"
    };

    private static string GetIconForType(string type) => type switch
    {
        "Success" => "bi-check-circle-fill",
        "Warning" => "bi-exclamation-triangle-fill",
        "Error"   => "bi-x-circle-fill",
        _         => "bi-info-circle-fill"
    };

    private static string FormatTimeAgo(DateTime createdAt)
    {
        var diff = DateTime.UtcNow - createdAt;
        if (diff.TotalMinutes < 1) return "vừa xong";
        if (diff.TotalHours < 1) return $"{(int)diff.TotalMinutes} phút trước";
        if (diff.TotalDays < 1) return $"{(int)diff.TotalHours} giờ trước";
        if (diff.TotalDays < 7) return $"{(int)diff.TotalDays} ngày trước";
        return createdAt.ToLocalTime().ToString("dd/MM/yyyy");
    }

    private UserNotificationDto MapToUserDto(Notifications n) => new()
    {
        NotificationId = n.NotificationId,
        Title = n.Title,
        Message = n.Message,
        Type = n.Type,
        IsRead = n.IsRead,
        CreatedAt = n.CreatedAt,
        RelatedEntity = n.RelatedEntity,
        RelatedEntityId = n.RelatedEntityId,
        IconClass = GetIconForType(n.Type),
        TimeAgo = FormatTimeAgo(n.CreatedAt),
        CategoryLabel = GetCategoryLabel(n.RelatedEntity)
    };
}
