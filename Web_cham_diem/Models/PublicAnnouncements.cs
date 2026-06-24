namespace Web_cham_diem.Models
{
    public class PublicAnnouncements
    {
        public int AnnouncementId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string? AttachmentUrl { get; set; }
        public string? AttachmentFileName { get; set; }
        public string Type { get; set; } = "Info"; // Info, Warning, Success, Error
        public bool IsPublished { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? CreatedByUserId { get; set; }
        public int? RelatedCompetitionId { get; set; }

        public Users? CreatedBy { get; set; }
    }
}
