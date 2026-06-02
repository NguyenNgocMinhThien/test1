namespace Web_cham_diem.Models
{
    public class CompetitionDocuments
    {
        public int DocumentId { get; set; }
        public int CompetitionId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty; // pdf, docx, xlsx, pptx, etc.
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Foreign Keys
        public Competitions Competition { get; set; } = null!;
    }
}
