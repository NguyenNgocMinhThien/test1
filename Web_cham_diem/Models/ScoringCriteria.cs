namespace Web_cham_diem.Models
{
    public class ScoringCriteria
    {
        public int CriteriaId { get; set; }
        public int CompetitionId { get; set; }
        public string CriteriaName { get; set; } = string.Empty; // Ý tưởng, Kỹ thuật, Thuyết trình, v.v.
        public string? Description { get; set; }
        public decimal MaxScore { get; set; } = 10; // Điểm tối đa cho tiêu chí này
        public decimal Weight { get; set; } = 1.0m; // Trọng số (1.0 = 100%)
        public int Order { get; set; } = 0; // Thứ tự hiển thị
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Foreign Keys
        public Competitions Competition { get; set; } = null!;

        // Navigation properties
        public ICollection<Scores> Scores { get; set; } = new List<Scores>();
    }
}
