namespace Web_cham_diem.Models.ViewModels
{
    public class OrganizerContestsViewModel
    {
        // Statistics
        public int TotalCompetitions { get; set; }
        public int ActiveCompetitions { get; set; }
        public int UpcomingCompetitions { get; set; }
        public int ClosedCompetitions { get; set; }

        // List of competitions
        public List<CompetitionOrganizerDto> Competitions { get; set; } = new();

        // Filter & Search
        public string? SearchQuery { get; set; }
        public string? StatusFilter { get; set; } = "all";
        public string? CategoryFilter { get; set; } = "all";
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 12;
    }

    public class CompetitionOrganizerDto
    {
        public int CompetitionId { get; set; }
        public string CompetitionName { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? BannerImageUrl { get; set; }
        public string Status { get; set; } = string.Empty; // Draft, Active, Closed, Completed
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime RegistrationDeadline { get; set; }
        public DateTime SubmissionDeadline { get; set; }

        // Statistics for card
        public int TotalRegistrations { get; set; }
        public int ApprovedRegistrations { get; set; }
        public int TotalSubmissions { get; set; }
        public int EvaluatedSubmissions { get; set; }
        public int MaxParticipants { get; set; }
        public int MaxTeamSize { get; set; }

        // Progress
        public int ProgressPercentage { get; set; }
        public string CurrentPhase { get; set; } = string.Empty;
        public string StatusDisplay { get; set; } = string.Empty; // "Đang Nhận Bài", "Đang Chấm Điểm", etc.
        public bool IsTeamBased { get; set; }
    }

    public class CompetitionDetailViewModel
    {
        public int CompetitionId { get; set; }
        public string CompetitionName { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? Description { get; set; }
        public string? Rules { get; set; }
        public string? Prize { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime RegistrationDeadline { get; set; }
        public DateTime SubmissionDeadline { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsTeamBased { get; set; }
        public int MaxParticipants { get; set; }
        public int MaxTeamSize { get; set; }
        public int TotalRegistrations { get; set; }
        public int ApprovedRegistrations { get; set; }
        public int TotalSubmissions { get; set; }
        public List<ScoringCriteriaDto> ScoringCriteria { get; set; } = new();
    }

    public class ScoringCriteriaDto
    {
        public int CriteriaId { get; set; }
        public string CriteriaName { get; set; } = string.Empty;
        public decimal Weight { get; set; }
        public decimal MaxScore { get; set; }
    }
}