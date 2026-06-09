namespace Web_cham_diem.Models
{
    public class Judges
    {
        public int JudgeId { get; set; }
        public int UserId { get; set; }
        public int CompetitionId { get; set; }
        public int? RoundId { get; set; }                  // Vòng thi được phân công (null = chưa gán vòng)
        public string JudgeRole { get; set; } = "Member";  // HeadJudge | ViceHead | Member
        public string Expertise { get; set; } = string.Empty;
        public int Priority { get; set; } = 0;
        public string Status { get; set; } = "Active";     // Active | Inactive
        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Foreign Keys
        public Users User { get; set; } = null!;
        public Competitions Competition { get; set; } = null!;
        public CompetitionRounds? Round { get; set; }

        // Navigation properties
        public ICollection<Scores> Scores { get; set; } = new List<Scores>();
        public ICollection<JudgeAssignments> JudgeAssignments { get; set; } = new List<JudgeAssignments>();
    }
}
