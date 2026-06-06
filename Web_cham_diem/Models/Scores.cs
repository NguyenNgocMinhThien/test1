namespace Web_cham_diem.Models
{
    public class Scores
    {
        public int ScoreId { get; set; }
        public int SubmissionId { get; set; }
        public int JudgeId { get; set; }
        public int CriteriaId { get; set; }
        public decimal Score { get; set; } // Điểm chấm
        public string? Comment { get; set; } // Nhận xét
        public DateTime ScoredDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Approval workflow (trưởng ban duyệt điểm)
        public string ApprovalStatus { get; set; } = "Pending"; // Pending | Approved | Rejected
        public int? ApprovedBy { get; set; } // UserId trưởng ban giám khảo
        public DateTime? ApprovedAt { get; set; }
        public string? RejectionReason { get; set; }

        // Foreign Keys
        public Submissions Submission { get; set; } = null!;
        public Judges Judge { get; set; } = null!;
        public ScoringCriteria Criteria { get; set; } = null!;
        public Users? Approver { get; set; }
    }
}
