namespace Web_cham_diem.Models.ViewModels
{
    public class OrganizerGradingViewModel
    {
        // Thống kê
        public int ActiveJudges { get; set; }
        public int TotalAssignments { get; set; }
        public int CompletedAssignments { get; set; }
        public double CompletionRate { get; set; }

        // Dữ liệu
        public List<JudgeOverviewDto> Judges { get; set; } = new();
        public List<SubmissionForAssignmentDto> Submissions { get; set; } = new();
        public List<ScoringCriteriaViewDto> ScoringCriteria { get; set; } = new();
        public List<AssignmentDetailDto> Assignments { get; set; } = new();
        public List<JudgeAlertDto> Alerts { get; set; } = new();

        // Filters
        public int? SelectedCompetitionId { get; set; }
        public List<CompetitionBasicDto> Competitions { get; set; } = new();
    }

    public class JudgeOverviewDto
    {
        public int JudgeId { get; set; }
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Expertise { get; set; }
        public string JudgeRole { get; set; } = "Member";
        public string Status { get; set; } = "Active";
        public DateTime AssignedDate { get; set; }
        public int TotalAssigned { get; set; }
        public int Completed { get; set; }
        public int InProgress { get; set; }
        public double CompletionRate { get; set; }
    }

    public class SubmissionForAssignmentDto
    {
        public int SubmissionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string TeamOrRepresentative { get; set; } = string.Empty;
        public string SubmissionStatus { get; set; } = string.Empty;
        public List<AssignmentInfoDto> CurrentAssignments { get; set; } = new();
    }

    public class AssignmentInfoDto
    {
        public int AssignmentId { get; set; }
        public int JudgeId { get; set; }
        public string JudgeName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? GradingDeadline { get; set; }
    }

    public class AssignmentDetailDto
    {
        public int AssignmentId { get; set; }
        public int JudgeId { get; set; }
        public string JudgeName { get; set; } = string.Empty;
        public string JudgeRole { get; set; } = string.Empty;
        public int SubmissionId { get; set; }
        public string SubmissionTitle { get; set; } = string.Empty;
        public string TeamOrRep { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime AssignedDate { get; set; }
        public DateTime? GradingDeadline { get; set; }
        public bool IsOverdue { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class ScoringCriteriaViewDto
    {
        public int CriteriaId { get; set; }
        public string CriteriaName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal MaxScore { get; set; }
        public decimal Weight { get; set; }
        public int Order { get; set; }
    }

    public class JudgeAlertDto
    {
        public int JudgeId { get; set; }
        public string JudgeName { get; set; } = string.Empty;
        public int TotalAssigned { get; set; }
        public int Completed { get; set; }
        public double CompletionRate { get; set; }
        public DateTime? NearestDeadline { get; set; }
        public int HoursUntilDeadline { get; set; }
        public bool IsOverdue { get; set; }
    }

    // DTOs for AJAX requests
    public class AddJudgeDto
    {
        public int UserId { get; set; }
        public string Role { get; set; } = "Member";
        public string Expertise { get; set; } = string.Empty;
    }

    public class UpdateJudgeRoleDto
    {
        public string Role { get; set; } = "Member";
        public string Expertise { get; set; } = string.Empty;
    }

    public class AssignSubmissionsDto
    {
        public List<int> SubmissionIds { get; set; } = new();
        public int JudgeId { get; set; }
        public DateTime? GradingDeadline { get; set; }
    }

    public class UserSearchDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? StudentId { get; set; }
    }
}
