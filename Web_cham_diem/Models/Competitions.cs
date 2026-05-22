namespace Web_cham_diem.Models
{
    public class Competitions
    {
        public int CompetitionId { get; set; }
        public string CompetitionName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Category { get; set; } // Lĩnh vực thi
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime RegistrationDeadline { get; set; }
        public DateTime SubmissionDeadline { get; set; }
        public int MaxParticipants { get; set; }
        public int MaxTeamSize { get; set; }
        public decimal MaxScore { get; set; } = 100;
        public string Status { get; set; } = "Draft"; // Draft, Active, Closed, Completed
        public bool IsTeamBased { get; set; } = false;
        public string? Rules { get; set; }
        public string? Prize { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<Registrations> Registrations { get; set; } = new List<Registrations>();
        public ICollection<Teams> Teams { get; set; } = new List<Teams>();
        public ICollection<Submissions> Submissions { get; set; } = new List<Submissions>();
        public ICollection<ScoringCriteria> ScoringCriteria { get; set; } = new List<ScoringCriteria>();
    }
}
