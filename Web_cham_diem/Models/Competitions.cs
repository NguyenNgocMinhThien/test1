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
        public DateTime SubmissionDeadline { get; set; }
        public int MinParticipants { get; set; } = 0;
        public int MaxParticipants { get; set; }
        public int MaxTeamSize { get; set; }
        public decimal MaxScore { get; set; } = 100;
        public string Status { get; set; } = "Draft"; // Draft, Active, Closed, Completed
        public bool IsTeamBased { get; set; } = false;
        public string? Rules { get; set; }
        public string? Prize { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public int? CreatedByUserId { get; set; }

        // Navigation properties
        public Users? CreatedBy { get; set; }
        public ICollection<Registrations> Registrations { get; set; } = new List<Registrations>();
        public ICollection<Teams> Teams { get; set; } = new List<Teams>();
        public ICollection<Submissions> Submissions { get; set; } = new List<Submissions>();
        public ICollection<Judges> Judges { get; set; } = new List<Judges>();
        public ICollection<CompetitionImages> CompetitionImages { get; set; } = new List<CompetitionImages>();
        public ICollection<CompetitionDocuments> CompetitionDocuments { get; set; } = new List<CompetitionDocuments>();
        public ICollection<RegistrationRounds> RegistrationRounds { get; set; } = new List<RegistrationRounds>();
        public ICollection<CompetitionSponsors> CompetitionSponsors { get; set; } = new List<CompetitionSponsors>();
        public ICollection<CompetitionRounds> CompetitionRounds { get; set; } = new List<CompetitionRounds>();
        public ICollection<JudgeAssignments> JudgeAssignments { get; set; } = new List<JudgeAssignments>();
        public ICollection<RegistrationFormFields> RegistrationFormFields { get; set; } = new List<RegistrationFormFields>();
    }
}

