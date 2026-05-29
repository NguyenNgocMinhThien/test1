using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Web_cham_diem.Models.ViewModels
{
    public class CompetitionRegistrationViewModel
    {
        // Competition info (read-only for display)
        public int CompetitionId { get; set; }
        public string CompetitionName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime RegistrationDeadline { get; set; }
        public DateTime SubmissionDeadline { get; set; }
        public bool IsTeamBased { get; set; }
        public int MaxTeamSize { get; set; }
        public int MaxParticipants { get; set; }

        // Student info (from user claims/db)
        public string FullName { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // Form fields
        [Required(ErrorMessage = "Vui lòng chọn hình thức đăng ký.")]
        public string RegistrationType { get; set; } = "Individual"; // Individual, Team

        public string? TeamName { get; set; }
        public string? TeamMembers { get; set; } // Mỗi thành viên trên 1 dòng

        [MaxLength(1000)]
        public string? Notes { get; set; }

        // Files (stored into SubmissionDocument as list path)
        public IFormFile? CvFile { get; set; }
        public IFormFile? ProposalFile { get; set; }
        public IFormFile? ProjectFile { get; set; }

        [Range(typeof(bool), "true", "true", ErrorMessage = "Bạn cần xác nhận đồng ý thể lệ.")]
        public bool AcceptRules { get; set; }

        [Range(typeof(bool), "true", "true", ErrorMessage = "Bạn cần xác nhận thông tin chính xác.")]
        public bool ConfirmInformation { get; set; }
    }

    public class RegistrationMemberDto
    {
        public string FullName { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // Leader, Member, Tech, Designer
    }

    public class RegistrationFileDto
    {
        public string FileName { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty; // PDF, Slide, Image, Video
        public long FileSize { get; set; }
        public string? PreviewUrl { get; set; }
    }
}
