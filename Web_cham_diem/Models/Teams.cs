namespace Web_cham_diem.Models
{
    public class Teams
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public int CompetitionId { get; set; }
        public int LeaderId { get; set; } // UserId của trưởng đội
        public string? Description { get; set; }
        public string Status { get; set; } = "Active"; // Active, Disbanded
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Foreign Keys
        public Competitions Competition { get; set; } = null!;
        public Users Leader { get; set; } = null!;

        // Navigation properties
        public ICollection<TeamMembers> TeamMembers { get; set; } = new List<TeamMembers>();
        public ICollection<TeamTasks> TeamTasks { get; set; } = new List<TeamTasks>();
        public ICollection<Registrations> Registrations { get; set; } = new List<Registrations>();
        public ICollection<Submissions> Submissions { get; set; } = new List<Submissions>();
    }
}
