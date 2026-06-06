namespace Web_cham_diem.Models
{
    public class TeamMembers
    {
        public int TeamMemberId { get; set; }
        public int TeamId { get; set; }
        public int UserId { get; set; }
        public string Role { get; set; } = "Member"; // Leader | Member
        public string Status { get; set; } = "Active"; // Active | Left | Removed
        public int? InvitedBy { get; set; } // UserId người mời (đội trưởng hoặc giảng viên)
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        // Foreign Keys
        public Teams Team { get; set; } = null!;
        public Users User { get; set; } = null!;
        public Users? Inviter { get; set; }
    }
}
