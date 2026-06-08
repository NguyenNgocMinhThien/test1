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
        public DateTime? LatestRegistrationDeadline { get; set; } // EndDate của round cuối cùng
        public DateTime SubmissionDeadline { get; set; }
        public bool IsTeamBased { get; set; }
        public int MaxTeamSize { get; set; }
        public int MaxParticipants { get; set; }

        // Student info (from user claims/db)
        public string FullName { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // Set by controller based on logged-in role
        public bool IsLecturerFlow { get; set; } = false;

        // Lecturer flow only: MSSV của thí sinh đăng ký chính (đội trưởng)
        public string? LeaderStudentId { get; set; }

        // Form fields
        [Required(ErrorMessage = "Vui lòng chọn hình thức đăng ký.")]
        public string RegistrationType { get; set; } = "Individual"; // Individual, Team

        public string? TeamName { get; set; }

        // Thành viên đội (list MSSV, gửi qua hidden inputs)
        public List<string> MemberStudentIds { get; set; } = new();

        // MSSV của giảng viên hướng dẫn (tuỳ chọn)
        public string? AdvisorStudentId { get; set; }

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

    public class EditRegistrationViewModel
    {
        [MaxLength(1000)]
        public string? Notes { get; set; }

        // MSSV giảng viên hướng dẫn (để trống = xóa advisor)
        public string? AdvisorStudentId { get; set; }

        // Tên nhóm (chỉ dùng cho đăng ký nhóm)
        [MaxLength(200)]
        public string? TeamName { get; set; }

        // Danh sách đường dẫn file cần xóa (path từ SubmissionDocument)
        public List<string> RemoveFiles { get; set; } = new();

        // File đính kèm mới (tối đa 3 file)
        public IFormFile? NewFile1 { get; set; }
        public IFormFile? NewFile2 { get; set; }
        public IFormFile? NewFile3 { get; set; }
    }
}
