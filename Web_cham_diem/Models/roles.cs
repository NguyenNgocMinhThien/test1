namespace Web_cham_diem.Models
{
    public class Roles
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty; // Admin, Student, Organizer, Judge, Lecturer
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation property
        public ICollection<Users> Users { get; set; } = new List<Users>();
    }
}
