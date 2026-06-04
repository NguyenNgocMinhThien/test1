namespace Web_cham_diem.Models
{
    public class RegistrationRounds
    {
        public int RoundId { get; set; }
        public int CompetitionId { get; set; }
        public string RoundName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign Keys
        public Competitions Competition { get; set; } = null!;

        // Navigation properties
        public ICollection<Registrations> Registrations { get; set; } = new List<Registrations>();
    }
}
