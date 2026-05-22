namespace Web_cham_diem.Models
{
    public class Notifications
    {
        public int NotificationId { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = "Info"; // Info, Warning, Success, Error
        public string? RelatedEntity { get; set; } // Competition, Registration, Submission, Score
        public int? RelatedEntityId { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; set; }

        // Foreign Keys
        public Users User { get; set; } = null!;
    }
}
