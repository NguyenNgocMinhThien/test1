namespace Web_cham_diem.Models
{
    public class Judges
    {
        public int JudgeId { get; set; }
        public int UserId { get; set; }
        public int CompetitionId { get; set; }
        public string Expertise { get; set; } = string.Empty; // Lĩnh vực chuyên môn
        public int Priority { get; set; } = 0; // Mức độ ưu tiên (0=cao, 1=trung bình, 2=thấp)
        public string Status { get; set; } = "Active"; // Active, Inactive
        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Foreign Keys
        public Users User { get; set; } = null!;
        public Competitions Competition { get; set; } = null!;

        // Navigation properties
        public ICollection<Scores> Scores { get; set; } = new List<Scores>();
        public ICollection<JudgeAssignments> JudgeAssignments { get; set; } = new List<JudgeAssignments>();
    }
}
