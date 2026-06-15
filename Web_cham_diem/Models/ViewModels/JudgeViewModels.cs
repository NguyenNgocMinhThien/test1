namespace Web_cham_diem.Models.ViewModels
{
    public class JudgeDashboardViewModel
    {
        public int TotalAssigned { get; set; }
        public int TotalCompleted { get; set; }
        public int TotalPending { get; set; }
        public int TotalInProgress { get; set; }
        public List<JudgeCompetitionGroupDto> CompetitionGroups { get; set; } = new();
    }

    public class JudgeCompetitionGroupDto
    {
        public int CompetitionId { get; set; }
        public string CompetitionName { get; set; } = "";
        public string JudgeRole { get; set; } = "Member";
        public string? RoundName { get; set; }
        public int TotalAssigned { get; set; }
        public int Completed { get; set; }
        public List<JudgeAssignmentRowDto> Assignments { get; set; } = new();
    }

    public class JudgeAssignmentRowDto
    {
        public int AssignmentId { get; set; }
        public int SubmissionId { get; set; }
        public string SubmissionTitle { get; set; } = "";
        public string TeamOrRep { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime? GradingDeadline { get; set; }
        public bool IsOverdue { get; set; }
        public bool AlreadyScored { get; set; }
    }

    public class JudgeGradeViewModel
    {
        public int AssignmentId { get; set; }
        public string AssignmentStatus { get; set; } = "";
        public DateTime? GradingDeadline { get; set; }

        public int SubmissionId { get; set; }
        public string SubmissionTitle { get; set; } = "";
        public string? SubmissionDescription { get; set; }
        public string? FileUrl { get; set; }
        public string? VideoUrl { get; set; }
        public string? ProjectLink { get; set; }
        public string SubmissionStatus { get; set; } = "";
        public string TeamOrRep { get; set; } = "";

        public int CompetitionId { get; set; }
        public string CompetitionName { get; set; } = "";
        public string? RoundName { get; set; }

        public int JudgeId { get; set; }
        public bool AlreadyScored { get; set; }
        public bool IsRejected { get; set; }
        public string? RejectionReason { get; set; }
        public List<CriteriaScoreInputDto> Criteria { get; set; } = new();
    }

    public class CriteriaScoreInputDto
    {
        public int CriteriaId { get; set; }
        public string CriteriaName { get; set; } = "";
        public string? Description { get; set; }
        public decimal MaxScore { get; set; }
        public decimal Weight { get; set; }
        public int Order { get; set; }
        public decimal? Score { get; set; }
        public string? Comment { get; set; }
    }

    // ── Review / Approval ViewModels ─────────────────────────────────────────

    public class JudgeReviewViewModel
    {
        public List<RoundReviewDto> Rounds { get; set; } = new();
    }

    public class RoundReviewDto
    {
        public int RoundId { get; set; }
        public string RoundName { get; set; } = "";
        public string CompetitionName { get; set; } = "";
        public int HeadJudgeId { get; set; }
        public int PendingApprovalCount { get; set; }
        public int ApprovedCount { get; set; }
        public int TotalJudgeScoreGroups { get; set; }
        public List<SubmissionApprovalDto> Submissions { get; set; } = new();
    }

    public class SubmissionApprovalDto
    {
        public int SubmissionId { get; set; }
        public string Title { get; set; } = "";
        public string TeamOrRep { get; set; } = "";
        public bool AllApproved { get; set; }
        public List<JudgeScoreGroupDto> JudgeScoreGroups { get; set; } = new();
    }

    public class JudgeScoreGroupDto
    {
        public int JudgeId { get; set; }
        public int UserId { get; set; }
        public string JudgeName { get; set; } = "";
        public string JudgeRole { get; set; } = "";
        public bool IsCurrentHeadJudge { get; set; }
        // NotScored | Pending | Approved | Rejected
        public string OverallApprovalStatus { get; set; } = "NotScored";
        public string? RejectionReason { get; set; }
        public List<ScoreDetailDto> Scores { get; set; } = new();
    }

    public class ScoreDetailDto
    {
        public int ScoreId { get; set; }
        public string CriteriaName { get; set; } = "";
        public decimal MaxScore { get; set; }
        public decimal Weight { get; set; }
        public decimal Score { get; set; }
        public string? Comment { get; set; }
    }

    public record ApproveScoresRequest(int JudgeId, int SubmissionId, string Action, string? RejectionReason);
}
