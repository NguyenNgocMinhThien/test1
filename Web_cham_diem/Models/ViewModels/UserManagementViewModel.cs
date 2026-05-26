using Microsoft.AspNetCore.Mvc;

namespace Web_cham_diem.Models.ViewModels
{
    public class UserManagementViewModel
    {
        public List<UserListItemDto> Users { get; set; } = new();
        public UserStatsDto Statistics { get; set; } = new();

        // Filter & Search properties
        public string? SearchQuery { get; set; }
        public int? RoleFilter { get; set; }
        public string? StatusFilter { get; set; }
        public string? DepartmentFilter { get; set; }
    }

    public class UserListItemDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? StudentId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Status { get; set; } = "Active"; // Active, Locked, Pending
        public DateTime CreatedAt { get; set; }
        public string? AvatarUrl { get; set; }
    }

    public class UserStatsDto
    {
        public int TotalUsers { get; set; }
        public int StudentCount { get; set; }
        public int LecturerCount { get; set; }
        public int OrganizerCount { get; set; }
        public int JudgeCount { get; set; }
        public int LockedCount { get; set; }
    }
}