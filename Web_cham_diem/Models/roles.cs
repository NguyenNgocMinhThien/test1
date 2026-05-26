namespace Web_cham_diem.Models
{
    public class Roles
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<UserRoles> UserRoles { get; set; } = new List<UserRoles>();
    }
}
