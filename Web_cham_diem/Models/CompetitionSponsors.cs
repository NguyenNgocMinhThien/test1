namespace Web_cham_diem.Models
{
    public class CompetitionSponsors
    {
        public int CompetitionSponsorId { get; set; }
        public int CompetitionId { get; set; }
        public int SponsorId { get; set; }

        // Mức độ tài trợ
        public string SponsorshipLevel { get; set; } = "Gold"; // Gold, Silver, Bronze, General
        public decimal ContributionAmount { get; set; } = 0; // Số tiền tài trợ
        public string Currency { get; set; } = "VND"; // Đơn vị tiền tệ

        // Thông tin chi tiết
        public string? Notes { get; set; }
        public bool IsDisplayed { get; set; } = true; // Hiển thị công khai
        public int DisplayOrder { get; set; } = 0; // Thứ tự hiển thị

        public DateTime SponsoredAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Foreign Keys
        public Competitions Competition { get; set; } = null!;
        public Sponsors Sponsor { get; set; } = null!;
    }
}