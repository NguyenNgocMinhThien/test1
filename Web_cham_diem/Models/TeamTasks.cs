namespace Web_cham_diem.Models
{
    public class TeamTasks
    {
        public int TaskId { get; set; }
        public int TeamId { get; set; }
        public int AssignedBy { get; set; } // UserId (giảng viên hoặc đội trưởng)
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? DueDate { get; set; }
        public string Status { get; set; } = "Pending"; // Pending | InProgress | Completed
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Foreign Keys
        public Teams Team { get; set; } = null!;
        public Users AssignedByUser { get; set; } = null!;

        // Navigation
        public ICollection<TaskCompletions> TaskCompletions { get; set; } = new List<TaskCompletions>();
    }
}
