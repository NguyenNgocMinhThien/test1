namespace Web_cham_diem.Models
{
    public class CompetitionRounds
    {
        public int RoundId { get; set; }
        public int CompetitionId { get; set; }

        public string RoundName { get; set; } = string.Empty;      // "Sơ khảo", "Bán kết", "Chung kết"
        public int RoundOrder { get; set; } = 1;                    // Thứ tự sắp xếp (1, 2, 3...)
        public string? Description { get; set; }                    // Mô tả yêu cầu của vòng

        public DateTime StartDate { get; set; }                     // Ngày bắt đầu vòng thi
        public DateTime EndDate { get; set; }                       // Ngày kết thúc vòng thi
        public DateTime? SubmissionDeadline { get; set; }           // Hạn nộp bài trong vòng này

        public int? MaxAdvancing { get; set; }                      // Số người/đội vào vòng tiếp (null = tất cả)
        public string Status { get; set; } = "Upcoming";            // Upcoming | Active | Completed

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Foreign Keys
        public Competitions Competition { get; set; } = null!;

        // Navigation
        public ICollection<Submissions> Submissions { get; set; } = new List<Submissions>();
        public ICollection<JudgeAssignments> JudgeAssignments { get; set; } = new List<JudgeAssignments>();
    }
}
