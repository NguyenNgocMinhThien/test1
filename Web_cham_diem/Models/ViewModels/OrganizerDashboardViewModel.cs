namespace Web_cham_diem.Models.ViewModels
{
    public class OrganizerDashboardViewModel
    {
        // ===== STATISTICS =====
        public int ActiveCompetitions { get; set; }
        public int PendingRegistrations { get; set; }
        public int TotalSubmissions { get; set; }
        public int EvaluatedSubmissions { get; set; }
        public int ActiveJudges { get; set; }
        public int UrgentCompetitions { get; set; } // Sắp kết thúc

        // ===== CHART DATA =====
        public List<CompetitionProgressData> ProgressData { get; set; } = new();
        public ApprovalRatioData ApprovalRatio { get; set; } = new();

        // ===== DEADLINES & TIMELINE =====
        public List<DeadlineItem> UpcomingDeadlines { get; set; } = new();

        // ===== RECENT ACTIVITIES =====
        public List<ActivityLog> RecentActivities { get; set; } = new();
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
        public string Title { get; set; } = string.Empty; // "Đóng cổng Nhận bài", "Hạn nộp điểm", etc.
        public DateTime DeadlineDate { get; set; }
        public string Status { get; set; } = string.Empty; // "urgent", "warning", "normal"
        public decimal? ProgressPercentage { get; set; } // Dùng cho hiển thị tiến độ (chấm điểm)
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
