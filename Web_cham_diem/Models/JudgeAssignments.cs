namespace Web_cham_diem.Models
{
    public class JudgeAssignments
    {
        public int AssignmentId { get; set; }
        public int JudgeId { get; set; }                // FK → Judges (giám khảo được phân công)
        public int SubmissionId { get; set; }           // FK → Submissions (bài thi cần chấm)
        public int CompetitionId { get; set; }          // FK → Competitions (để query nhanh, tránh join thừa)
        public int? RoundId { get; set; }               // FK → CompetitionRounds (vòng thi)
        public int AssignedByUserId { get; set; }       // FK → Users (Organizer thực hiện phân công)

        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;
        public DateTime? GradingDeadline { get; set; }  // Hạn chót giám khảo phải nộp điểm

        public string Status { get; set; } = "Pending"; // Pending | InProgress | Completed | Reassigned
        public string? Notes { get; set; }              // Ghi chú của BTC khi phân công

        public DateTime? CompletedAt { get; set; }      // Thời điểm giám khảo chấm xong
        public DateTime? UpdatedAt { get; set; }

        // Foreign Keys
        public Judges Judge { get; set; } = null!;
        public Submissions Submission { get; set; } = null!;
        public Competitions Competition { get; set; } = null!;
        public CompetitionRounds? Round { get; set; }
        public Users AssignedByUser { get; set; } = null!;
    }
}
