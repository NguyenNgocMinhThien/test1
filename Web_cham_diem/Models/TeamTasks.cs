namespace Web_cham_diem.Models
{
    public class TeamTasks
    {
        public int TaskId { get; set; }
        public int TeamId { get; set; }
        public int AssignedBy { get; set; }   // UserId người giao task (Lecturer/Leader)
        public int? AssignedTo { get; set; }  // UserId thành viên được giao (null = cả nhóm)
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? DueDate { get; set; }
        public string Status { get; set; } = "Pending"; // Pending | InProgress | Completed
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Foreign Keys
        public Teams Team { get; set; } = null!;
        public Users AssignedByUser { get; set; } = null!;
        public Users? AssignedToUser { get; set; }

        // Navigation
        public ICollection<TaskCompletions> TaskCompletions { get; set; } = new List<TaskCompletions>();
    }
}
