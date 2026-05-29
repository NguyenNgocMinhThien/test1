namespace Web_cham_diem.Models.ViewModels;

public class CreateUserViewModel
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty; // Nếu cần dùng lưu vào bảng dữ liệu khác sau này
    public string Password { get; set; } = string.Empty;
}