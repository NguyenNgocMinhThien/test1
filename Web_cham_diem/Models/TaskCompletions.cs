namespace Web_cham_diem.Models
{
    public class TaskCompletions
    {
        public int CompletionId { get; set; }
        public int TaskId { get; set; }
        public int CompletedBy { get; set; } // UserId thành viên đánh dấu hoàn thành
        public string? Note { get; set; }
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
        public bool IsVerified { get; set; } = false;
        public DateTime? VerifiedAt { get; set; }
        public int? VerifiedBy { get; set; } // UserId giảng viên xác nhận

        // Foreign Keys
        public TeamTasks Task { get; set; } = null!;
        public Users CompletedByUser { get; set; } = null!;
        public Users? VerifiedByUser { get; set; }
    }
}
