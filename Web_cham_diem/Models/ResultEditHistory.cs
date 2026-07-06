namespace Web_cham_diem.Models
{
    public class ResultEditHistory
    {
        public int HistoryId { get; set; }
        public int CompetitionId { get; set; }
        public int? RoundId { get; set; }
        public int? SubmissionId { get; set; }
        public int? JudgeId { get; set; }
        public int? CriteriaId { get; set; }

        public int EditedBy { get; set; }
        public DateTime EditedAt { get; set; } = DateTime.UtcNow;

        // "Publish" | "Unpublish" | "ScoreEdited" | "ScoreApproved" | "ScoreRejected"
        public string ActionType { get; set; } = string.Empty;

        // Tóm tắt hành động, hiển thị trực tiếp trong lịch sử
        public string ChangesSummary { get; set; } = string.Empty;

        // Giá trị điểm cũ / mới (dùng cho ScoreEdited)
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }

        // Navigation
        public Competitions Competition { get; set; } = null!;
        public CompetitionRounds? Round { get; set; }
        public Submissions? Submission { get; set; }
        public Judges? Judge { get; set; }
        public ScoringCriteria? Criteria { get; set; }
        public Users Editor { get; set; } = null!;
    }
}
