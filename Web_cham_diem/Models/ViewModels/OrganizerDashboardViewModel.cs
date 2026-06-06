namespace Web_cham_diem.Models.ViewModels
{
    public class OrganizerDashboardViewModel
    {
        // ===== COMPETITION STATUS BREAKDOWN =====
        public int TotalCompetitions { get; set; }
        public int DraftCompetitions { get; set; }
        public int OpenRegistrationCompetitions { get; set; }
        public int AcceptingSubmissions { get; set; }
        public int GradingCompetitions { get; set; }
        public int CompletedCompetitions { get; set; }

        // ===== ACTIVITY METRICS =====
        public int ActiveCompetitions { get; set; }
        public int PendingRegistrations { get; set; }
        public int TotalRegistrations { get; set; }
        public int TotalTeams { get; set; }
        public int TotalSubmissions { get; set; }
        public int EvaluatedSubmissions { get; set; }
        public int ActiveJudges { get; set; }
        public int UrgentCompetitions { get; set; }
        public decimal ScoringCompletionRate { get; set; }

        // ===== ALERTS =====
        public List<AlertItem> Alerts { get; set; } = new();

        // ===== COMPETITION PROGRESS TABLE =====
        public List<CompetitionProgressRow> CompetitionProgressTable { get; set; } = new();

        // ===== TOP COMPETITIONS =====
        public List<TopCompetitionItem> TopCompetitionsByRegistrations { get; set; } = new();

        // ===== CHART DATA =====
        public List<CompetitionProgressData> ProgressData { get; set; } = new();
        public ApprovalRatioData ApprovalRatio { get; set; } = new();

        // ===== DEADLINES & TIMELINE =====
        public List<DeadlineItem> UpcomingDeadlines { get; set; } = new();

        // ===== RECENT ACTIVITIES =====
        public List<ActivityLog> RecentActivities { get; set; } = new();
    }

    public class AlertItem
    {
        public string Level { get; set; } = "warning";   // "danger", "warning", "info"
        public string Icon { get; set; } = "bi-exclamation-triangle";
        public string Message { get; set; } = string.Empty;
        public string? ActionUrl { get; set; }
        public string? ActionText { get; set; }
    }

    public class CompetitionProgressRow
    {
        public int CompetitionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Phase { get; set; } = string.Empty;
        public int TotalRegistrations { get; set; }
        public int PendingRegistrations { get; set; }
        public int ApprovedRegistrations { get; set; }
        public int TotalSubmissions { get; set; }
        public int GradedSubmissions { get; set; }
        public decimal GradingProgress { get; set; }
        public bool UnderMinParticipants { get; set; }
        public int MinParticipants { get; set; }
        public string? ActiveRoundName { get; set; }
        public DateTime? ActiveRoundEnd { get; set; }
        public DateTime? NextRoundStart { get; set; }
    }

    public class TopCompetitionItem
    {
        public int CompetitionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int RegistrationCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Phase { get; set; } = string.Empty;
    }

    public class CompetitionProgressData
    {
        public string Week { get; set; } = string.Empty;
        public int Registrations { get; set; }
        public int Submissions { get; set; }
    }

    public class ApprovalRatioData
    {
        public int ApprovedCount { get; set; }
        public int PendingCount { get; set; }
        public int RejectedCount { get; set; }
        public int TotalProcessed => ApprovedCount + PendingCount + RejectedCount;
    }

    public class DeadlineItem
    {
        public int CompetitionId { get; set; }
        public string CompetitionName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateTime DeadlineDate { get; set; }
        public string Status { get; set; } = string.Empty; // "urgent", "warning", "normal"
        public decimal? ProgressPercentage { get; set; }
        public string? Description { get; set; }
    }

    public class ActivityLog
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty; // "score", "registration", "submission", "message"
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? UserName { get; set; }
    }
}
