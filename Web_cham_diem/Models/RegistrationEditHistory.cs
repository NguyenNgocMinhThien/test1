namespace Web_cham_diem.Models
{
    public class RegistrationEditHistory
    {
        public int HistoryId { get; set; }
        public int RegistrationId { get; set; }
        public int EditedBy { get; set; }
        public DateTime EditedAt { get; set; } = DateTime.UtcNow;

        // "StudentEdit" | "OrganizerApproved" | "OrganizerRejected" | "OrganizerSupplement"
        public string ActionType { get; set; } = "StudentEdit";

        // Tóm tắt những gì thay đổi
        public string? ChangesSummary { get; set; }

        public string? PreviousStatus { get; set; }
        public string? NewStatus { get; set; }

        // Navigation
        public Registrations Registration { get; set; } = null!;
        public Users Editor { get; set; } = null!;
    }
}
