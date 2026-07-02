namespace Web_cham_diem.Models.ViewModels
{
    public class OrganizerSubmissionsViewModel
    {
        // Thống kê tổng quan
        public int PendingRegistrations { get; set; }
        public int ApprovedRegistrations { get; set; }
        public int TotalSubmissions { get; set; }
        public int LateSubmissions { get; set; }

        // Dữ liệu hồ sơ đăng ký
        public List<RegistrationDetailDto> PendingRegistrationsList { get; set; } = new();
        public List<RegistrationDetailDto> AllRegistrationsList { get; set; } = new();

        // Dữ liệu bài nộp
        public List<SubmissionDetailDto> SubmissionsList { get; set; } = new();

        // Dữ liệu tiến độ
        public ProgressStatisticsDto ProgressStatistics { get; set; } = new();

        // Filters
        public int? SelectedCompetitionId { get; set; }
        public string? SearchQuery { get; set; }
        public string? StatusFilter { get; set; }
        public string? DepartmentFilter { get; set; }
        public string? RegistrationTypeFilter { get; set; }
        public List<CompetitionBasicDto> Competitions { get; set; } = new();
    }

    public class RegistrationDetailDto
    {
        public int RegistrationId { get; set; }
        public string RepresentativeName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? StudentId { get; set; }
        public string? Department { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public string? SubmissionDocument { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime RegistrationDate { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public string Notes { get; set; } = string.Empty;
        public int RegistrationId_FK { get; set; }
        public string? RegistrationType { get; set; }
        public string? AdvisorName { get; set; }
        public List<FormFieldValueDto> CustomFieldValues { get; set; } = new();
    }

    public class SubmissionDetailDto
    {
        public int SubmissionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty; // zip, pdf, etc
        public double FileSizeInMB { get; set; }
        public string RepresentativeName { get; set; } = string.Empty;
        public string TeamName { get; set; } = string.Empty;
        public string CompetitionName { get; set; } = string.Empty;
        public string? RoundName { get; set; }
        public DateTime SubmissionDate { get; set; }
        public bool IsLate { get; set; }
        public string Status { get; set; } = string.Empty; // Pending, Evaluated, etc
        public string? FileUrl { get; set; }
        public string? VideoUrl { get; set; }
        public string? ProjectLink { get; set; }
    }

    public class ProgressStatisticsDto
    {
        public int TotalExpected { get; set; }
        public int OnTimeSubmissions { get; set; }
        public int LateSubmissions { get; set; }
        public int NotSubmitted { get; set; }
        public double OnTimePercentage { get; set; }
        public double LatePercentage { get; set; }
        public double NotSubmittedPercentage { get; set; }
        public DateTime DeadlineDate { get; set; }
        public int HoursUntilDeadline { get; set; }
        public bool IsDeadlinePassing { get; set; }
    }

    public class CompetitionBasicDto
    {
        public int CompetitionId { get; set; }
        public string CompetitionName { get; set; } = string.Empty;
    }
}