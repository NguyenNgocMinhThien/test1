namespace Web_cham_diem.Models
{
    public class CompetitionImages
    {
        public int ImageId { get; set; }
        public int CompetitionId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsThumbnail { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign Keys
        public Competitions Competition { get; set; } = null!;
    }
}
