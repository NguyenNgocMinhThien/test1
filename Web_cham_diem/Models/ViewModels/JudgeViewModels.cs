namespace Web_cham_diem.Models.ViewModels
{
    public class JudgeDashboardViewModel
    {
        public int TotalAssigned { get; set; }
        public int TotalCompleted { get; set; }
        public int TotalPending { get; set; }
        public int TotalInProgress { get; set; }
        public double CompletionRate { get; set; }

        public int ApprovedScoresCount { get; set; }
        public int PendingScoresCount { get; set; }
        public int RejectedScoresCount { get; set; }

        public List<CriteriaSummaryDto> CriteriaSummaries { get; set; } = new();
        public List<RecentGradingDto> RecentGradings { get; set; } = new();
    }

    public class JudgeAssignmentsViewModel
    {
        public int TotalAssigned { get; set; }
        public int TotalCompleted { get; set; }
        public int TotalPending { get; set; }
        public int TotalInProgress { get; set; }
        public List<JudgeCompetitionGroupDto> CompetitionGroups { get; set; } = new();
    }

    public class CriteriaSummaryDto
    {
        public string CriteriaName { get; set; } = "";
        public decimal AverageScore { get; set; }
        public decimal MaxScore { get; set; }
        public int ScoredCount { get; set; }
        public int CommentCount { get; set; }
    }

    public class RecentGradingDto
    {
        public int AssignmentId { get; set; }
        public string SubmissionTitle { get; set; } = "";
        public string CompetitionName { get; set; } = "";
        public string? RoundName { get; set; }
        // Approved | Pending | Rejected
        public string ApprovalStatus { get; set; } = "";
        public decimal TotalScore { get; set; }
        public decimal MaxScore { get; set; }
        public string? JudgeComment { get; set; }
        public DateTime? CompletedAt { get; set; }
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
        public string? JudgeComment { get; set; }
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

    // ── Score History ViewModels ─────────────────────────────────────────────

    public class JudgeScoreHistoryViewModel
    {
        public int TotalScored { get; set; }
        public int ApprovedCount { get; set; }
        public int PendingCount { get; set; }
        public int RejectedCount { get; set; }
        public string? CompetitionFilter { get; set; }
        public string? StatusFilter { get; set; }
        public List<string> AvailableCompetitions { get; set; } = new();
        public List<ScoreHistoryItemDto> Items { get; set; } = new();
    }

    public class ScoreHistoryItemDto
    {
        public int AssignmentId { get; set; }
        public int SubmissionId { get; set; }
        public string SubmissionTitle { get; set; } = "";
        public string CompetitionName { get; set; } = "";
        public string? RoundName { get; set; }
        public string AssignmentStatus { get; set; } = "";
        // Pending | Approved | Rejected
        public string OverallApprovalStatus { get; set; } = "";
        public string? RejectionReason { get; set; }
        public DateTime? ScoredDate { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
        public DateTime? GradingDeadline { get; set; }
        public decimal TotalScore { get; set; }
        public decimal MaxPossibleScore { get; set; }
        // True khi assignment bị từ chối và reset về Pending → có thể chỉnh sửa
        public bool CanEdit { get; set; }
        public List<ScoreHistoryCriteriaDto> Criteria { get; set; } = new();
    }

    public class ScoreHistoryCriteriaDto
    {
        public string CriteriaName { get; set; } = "";
        public decimal Score { get; set; }
        public decimal MaxScore { get; set; }
        public decimal Weight { get; set; }
        public string? Comment { get; set; }
        public string ApprovalStatus { get; set; } = "";
    }
}
