namespace Web_cham_diem.Models
{
    public class Users
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? StudentId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastLogin { get; set; }

        public ICollection<UserRoles> UserRoles { get; set; } = new List<UserRoles>();

        public ICollection<Registrations> Registrations { get; set; } = new List<Registrations>();
        public ICollection<Judges> Judges { get; set; } = new List<Judges>();
        public ICollection<Notifications> Notifications { get; set; } = new List<Notifications>();
    }
}
