namespace Web_cham_diem.Models
{
    public class Registrations
    {
        public int RegistrationId { get; set; }
        public int UserId { get; set; }
        public int CompetitionId { get; set; }
        public int? TeamId { get; set; }
        public int? RoundId { get; set; }
        public string RegistrationType { get; set; } = "Individual"; // Individual, Team
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, Withdrawn
        public string? SubmissionDocument { get; set; } // Tài liệu hồ sơ
        public string? Notes { get; set; }
        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
        public DateTime? ApprovalDate { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Foreign Keys
        public Users User { get; set; } = null!;
        public Competitions Competition { get; set; } = null!;
        public Teams? Team { get; set; }
        public RegistrationRounds? RegistrationRound { get; set; }
    }
}
