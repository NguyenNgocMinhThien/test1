namespace Web_cham_diem.Models
{
    public class Sponsors
    {
        public int SponsorId { get; set; }
        public string SponsorName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Website { get; set; }
        public string? LogoUrl { get; set; }
        public string? Description { get; set; }
        public string Status { get; set; } = "Active"; // Active, Inactive
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<CompetitionSponsors> CompetitionSponsors { get; set; } = new List<CompetitionSponsors>();
    }
}
