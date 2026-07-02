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

        public bool IsResultsPublished { get; set; } = false;       // Ban tổ chức đã công bố kết quả vòng này ra trang công khai chưa
        public DateTime? ResultsPublishedAt { get; set; }           // Thời điểm công bố

        // Yêu cầu bài nộp
        public bool RequiresFile { get; set; } = true;              // Yêu cầu nộp file
        public bool RequiresVideo { get; set; } = false;            // Yêu cầu link video
        public bool RequiresOtherLink { get; set; } = false;        // Yêu cầu link khác

        // Thông tin buổi thi / thuyết trình
        public bool IsOnline { get; set; } = false;                 // Online hay offline
        public string? MeetingLink { get; set; }                    // Link tham dự online
        public DateTime? MeetingTime { get; set; }                  // Thời gian cuộc họp / bảo vệ
        public string? Location { get; set; }                       // Địa điểm (nếu offline)

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Foreign Keys
        public Competitions Competition { get; set; } = null!;

        // Navigation
        public ICollection<Submissions> Submissions { get; set; } = new List<Submissions>();
        public ICollection<JudgeAssignments> JudgeAssignments { get; set; } = new List<JudgeAssignments>();
        public ICollection<ScoringCriteria> ScoringCriteria { get; set; } = new List<ScoringCriteria>();
        public ICollection<Judges> Judges { get; set; } = new List<Judges>();
    }
}
