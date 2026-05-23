using System.ComponentModel.DataAnnotations;

namespace Web_cham_diem.Models;

public class RegisterTopicViewModel
{
    [Required(ErrorMessage = "Vui l?ng ch?n cu?c thi.")]
    public int CompetitionId { get; set; }

    [Required(ErrorMessage = "Vui l?ng ch?n lo?i ðãng k?.")]
    public string RegistrationType { get; set; } = "Individual"; // Individual, Team

    // Thông tin sinh viên cá nhân
    [Required(ErrorMessage = "Vui l?ng nh?p h? và tên.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui l?ng nh?p m? sinh viên.")]
    public string StudentCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui l?ng nh?p email.")]
    [EmailAddress(ErrorMessage = "Email không h?p l?.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui l?ng nh?p s? ði?n tho?i.")]
    [Phone(ErrorMessage = "S? ði?n tho?i không h?p l?.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui l?ng nh?p l?p h?c.")]
    public string Class { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui l?ng nh?p khoa/ngành.")]
    public string Faculty { get; set; } = string.Empty;

    // Thông tin ð?i thi
    public string? TeamName { get; set; }

    public string? TeamDescription { get; set; }

    [Range(1, 5, ErrorMessage = "S? thành viên ð?i ph?i t? 1 ð?n 5 ngý?i.")]
    public int? TeamMemberCount { get; set; }

    // H? sõ và tài li?u
    [Display(Name = "T?i lên h? sõ/tài li?u d? thi")]
    public IFormFile? SubmissionFile { get; set; }

    [Display(Name = "Ghi chú thêm")]
    public string? Notes { get; set; }

    // Hi?n th? thông tin cu?c thi
    public CompetitionDetailViewModel? Competition { get; set; }
}

public class CompetitionDetailViewModel
{
    public int CompetitionId { get; set; }
    public string CompetitionName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public DateTime RegistrationDeadline { get; set; }
    public DateTime SubmissionDeadline { get; set; }
    public int MaxTeamSize { get; set; }
    public bool IsTeamBased { get; set; }
    public string Status { get; set; } = string.Empty;
}
