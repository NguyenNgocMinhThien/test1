namespace Web_cham_diem.Models.ViewModels
{
    /// <summary>Tham số lọc/tìm kiếm cho trang Audit Logs.</summary>
    public class AuditLogFilter
    {
        public string? Search { get; set; }      // theo email, id, hoặc IP
        public string? Module { get; set; }
        public string? ActionType { get; set; }
        public DateTime? Date { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class AuditLogRowViewModel
    {
        public int LogId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string UserEmailSnapshot { get; set; } = string.Empty;
        public string UserRoleSnapshot { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public int? TargetId { get; set; }
        public string? Description { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public string? StatusDetail { get; set; }
    }

    public class AuditLogStatsViewModel
    {
        public int Logs24h { get; set; }
        public int LoginsToday { get; set; }
        public int SuspiciousCount { get; set; }
        public int FailedActionsToday { get; set; }
    }

    public class SecurityAlertViewModel
    {
        // "danger" | "warning" | "info"
        public string Severity { get; set; } = "info";
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; }
    }

    public class ActivityLogsViewModel
    {
        public AuditLogFilter Filter { get; set; } = new();
        public List<AuditLogRowViewModel> Logs { get; set; } = new();
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }
        public AuditLogStatsViewModel Stats { get; set; } = new();
        public List<SecurityAlertViewModel> Alerts { get; set; } = new();
    }
}