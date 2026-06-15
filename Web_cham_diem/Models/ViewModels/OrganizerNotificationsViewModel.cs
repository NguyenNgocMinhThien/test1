namespace Web_cham_diem.Models.ViewModels;

public class OrganizerNotificationsViewModel
{
    public int? SelectedCompetitionId { get; set; }
    public List<CompetitionBasicDto> Competitions { get; set; } = new();

    // Overall stats
    public int TotalSentAll { get; set; }
    public int UnreadByRecipients { get; set; }
    public int UpcomingMilestoneCount { get; set; }
    public int ActiveCompetitionCount { get; set; }

    // Milestone tracker for selected competition
    public CompetitionMilestoneDto? MilestoneStatus { get; set; }

    // Recent notification batches
    public List<NotificationBatchDto> RecentBatches { get; set; } = new();

    // Upcoming milestones across all competitions (next 30 days)
    public List<UpcomingMilestoneItemDto> UpcomingMilestones { get; set; } = new();
}

public class CompetitionMilestoneDto
{
    public int CompetitionId { get; set; }
    public string CompetitionName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int TotalRegistrations { get; set; }
    public int ApprovedRegistrations { get; set; }
    public int TotalJudges { get; set; }
    public List<MilestoneItemDto> Milestones { get; set; } = new();
}

public class MilestoneItemDto
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AudienceHint { get; set; } = string.Empty;
    public DateTime? Date { get; set; }
    public bool IsSent { get; set; }
    public DateTime? SentAt { get; set; }
    public int SentCount { get; set; }
    public string IconClass { get; set; } = "bi-bell";
    public string ThemeColor { get; set; } = "#6366f1";
    public string NotifType { get; set; } = "Info";
}

public class NotificationBatchDto
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? MilestoneKey { get; set; }
    public DateTime SentAt { get; set; }
    public int RecipientCount { get; set; }
    public int ReadCount { get; set; }
    public string CategoryLabel { get; set; } = string.Empty;
    public string CategoryCss { get; set; } = string.Empty;
}

public class UpcomingMilestoneItemDto
{
    public string Name { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string CompetitionName { get; set; } = string.Empty;
    public int CompetitionId { get; set; }
    public string MilestoneKey { get; set; } = string.Empty;
    public bool IsOverdue { get; set; }
    public int DaysUntil { get; set; }
    public bool NotificationSent { get; set; }
    public string BadgeCss { get; set; } = string.Empty;
    public string IconClass { get; set; } = string.Empty;
}

public class SendNotificationRequest
{
    public int CompetitionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "Info";
    public string Audience { get; set; } = "All";
    public string MilestoneKey { get; set; } = string.Empty;
}
