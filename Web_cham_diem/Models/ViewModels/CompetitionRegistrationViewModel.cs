using Microsoft.AspNetCore.Mvc;

namespace Web_cham_diem.Models.ViewModels
{
    public class CompetitionRegistrationViewModel
    {
        // Step 1: Cá nhân
        public string FullName { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Faculty { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string RegistrationType { get; set; } = "Individual"; // Individual, Team

        // Step 2: Bài dự thi
        public string TopicTitle { get; set; } = string.Empty;
        public string TopicDescription { get; set; } = string.Empty;
        public string Field { get; set; } = string.Empty;
        public string CompetitionType { get; set; } = string.Empty;

        // Step 3: Members (Dùng cho Team)
        public List<RegistrationMemberDto> Members { get; set; } = new();

        // Step 4: Files
        public List<RegistrationFileDto> Files { get; set; } = new();
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
