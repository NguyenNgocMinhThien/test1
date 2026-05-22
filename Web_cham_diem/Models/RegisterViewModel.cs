using System.ComponentModel.DataAnnotations;

namespace Web_cham_diem.Models;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Vui l?ng nh?p h? và tên.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui l?ng nh?p m? sinh viên.")]
    public string StudentCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui l?ng nh?p email.")]
    [EmailAddress(ErrorMessage = "Email chýa ðúng ð?nh d?ng.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui l?ng nh?p m?t kh?u.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui l?ng nh?p l?i m?t kh?u.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "M?t kh?u xác nh?n chýa kh?p.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
