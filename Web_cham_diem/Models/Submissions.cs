namespace Web_cham_diem.Models
{
    public class Submissions
    {
        public int SubmissionId { get; set; }
        public int CompetitionId { get; set; }
        public int? RegistrationId { get; set; } // Cho cá nhân
        public int? TeamId { get; set; } // Cho đội
        public int? CompetitionRoundId { get; set; } // FK → CompetitionRounds (thay thế int Round)
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? FileUrl { get; set; } // Link file/video/sản phẩm
        public string? VideoUrl { get; set; }
        public string? ProjectLink { get; set; }
        public string Status { get; set; } = "Draft"; // Draft, Submitted, Under Review, Evaluated
        public DateTime SubmissionDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public int? CreatedByUserId { get; set; } // Người tạo bài nộp đầu tiên (có thể là thành viên bất kỳ trong đội)
        public int? UpdatedByUserId { get; set; }  // Người thực hiện lần cập nhật gần nhất

        // Foreign Keys
        public Competitions Competition { get; set; } = null!;
        public Registrations? Registration { get; set; }
        public Teams? Team { get; set; }
        public CompetitionRounds? CompetitionRound { get; set; }
        public Users? CreatedByUser { get; set; }
        public Users? UpdatedByUser { get; set; }

        // Navigation properties
        public ICollection<Scores> Scores { get; set; } = new List<Scores>();
        public ICollection<JudgeAssignments> JudgeAssignments { get; set; } = new List<JudgeAssignments>();
    }
}
