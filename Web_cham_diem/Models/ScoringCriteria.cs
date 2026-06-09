namespace Web_cham_diem.Models
{
    public class ScoringCriteria
    {
        public int CriteriaId { get; set; }
        public int RoundId { get; set; }
        public string CriteriaName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal MaxScore { get; set; } = 10;
        public decimal Weight { get; set; } = 1.0m;
        public int Order { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Foreign Keys
        public CompetitionRounds Round { get; set; } = null!;

        // Navigation properties
        public ICollection<Scores> Scores { get; set; } = new List<Scores>();
    }
}
