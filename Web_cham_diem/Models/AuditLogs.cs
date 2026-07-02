namespace Web_cham_diem.Models
{
    /// <summary>
    /// Nhật ký hoạt động (Audit Log) - ghi lại mọi hành động quan trọng của người dùng
    /// trên hệ thống để phục vụ giám sát bảo mật và truy vết dữ liệu.
    /// </summary>
    public class AuditLogs
    {
        public int LogId { get; set; }

        // Người thực hiện hành động. Có thể null khi đăng nhập thất bại với email
        // không tồn tại trong hệ thống (không xác định được UserId).
        public int? UserId { get; set; }
        public Users? User { get; set; }

        // Lưu "ảnh chụp" (snapshot) email/role tại thời điểm ghi log, để log không
        // bị đổi nội dung nếu sau này user bị đổi tên, đổi quyền hoặc bị xóa.
        public string UserEmailSnapshot { get; set; } = string.Empty;
        public string UserRoleSnapshot { get; set; } = string.Empty;

        // CREATE | UPDATE | DELETE | LOGIN | LOGOUT
        public string ActionType { get; set; } = string.Empty;

        // Phân hệ: Auth | Contests | UserManagement | Submissions | Registrations | SystemSettings | Sponsors ...
        public string Module { get; set; } = string.Empty;

        // Id của đối tượng bị tác động (CompetitionId, UserId, RegistrationId, ...) nếu có
        public int? TargetId { get; set; }

        // Mô tả ngắn gọn hành động, ví dụ: "Xóa Cuộc thi ID #105"
        public string? Description { get; set; }

        public string IpAddress { get; set; } = string.Empty;

        public bool IsSuccess { get; set; } = true;

        // Chi tiết trạng thái, ví dụ: "Success" | "Failed (Wrong Pass)" | "Failed (Locked)"
        public string? StatusDetail { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}