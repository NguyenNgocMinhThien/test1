using System.ComponentModel.DataAnnotations;

namespace Web_cham_diem.Models.ViewModels
{
    public class CreateCompetitionViewModel
    {
        // Step 1: Thông tin cơ bản
        [Required(ErrorMessage = "Tên cuộc thi là bắt buộc.")]
        [StringLength(200, ErrorMessage = "Tên cuộc thi không được vượt quá 200 ký tự.")]
        public string CompetitionName { get; set; } = string.Empty;
        public string? Description { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn lĩnh vực.")]
        public string? Category { get; set; }
        public string? Rules { get; set; }
        public string? Prize { get; set; }

        // Step 2: Lịch trình & Hạn chế
        [Required]
        public DateTime StartDate { get; set; } = DateTime.Now.AddDays(15);

        [Required]
        public DateTime EndDate { get; set; } = DateTime.Now.AddDays(45);

        [Required]
        public DateTime SubmissionDeadline { get; set; } = DateTime.Now.AddDays(40);

        // MaxParticipants: nếu theo đội = số đội tối đa; nếu cá nhân = số thí sinh tối đa
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng tối đa phải lớn hơn 0.")]
        public int MaxParticipants { get; set; } = 100;

        // MaxTeamSize: chỉ dùng khi IsTeamBased = true (tối đa thành viên/đội, cho phép đội 1 người)
        [Range(1, int.MaxValue, ErrorMessage = "Số thành viên tối đa phải lớn hơn 0.")]
        public int MaxTeamSize { get; set; } = 5;
        public bool IsTeamBased { get; set; } = false;

        // Step 2b: Đợt đăng ký (bắt buộc ít nhất 1)
        public List<RegistrationRoundCreateDto> RegistrationRounds { get; set; } = new()
        {
            new RegistrationRoundCreateDto
            {
                RoundName = "Đợt 1",
                StartDate = DateTime.Now.AddDays(1),
                EndDate = DateTime.Now.AddDays(10)
            }
        };

        // Step 2c: Vòng thi (tùy chọn, có thể thêm nhiều vòng)
        public List<CompetitionRoundCreateDto> CompetitionRounds { get; set; } = new();

        // Step 3: Tiêu chí chấm điểm (bắt buộc ít nhất 1)
        public List<ScoringCriteriaCreateDto> ScoringCriteria { get; set; } = new()
        {
            new ScoringCriteriaCreateDto { CriteriaName = "Ý tưởng", MaxScore = 100, Weight = 0.30m, Order = 1 },
            new ScoringCriteriaCreateDto { CriteriaName = "Thực hiện", MaxScore = 100, Weight = 0.40m, Order = 2 },
            new ScoringCriteriaCreateDto { CriteriaName = "Thuyết trình", MaxScore = 100, Weight = 0.30m, Order = 3 }
        };

        // Step 4: Ảnh & Tài liệu (Không bắt buộc)
        public List<string>? SelectedImageData { get; set; } = new();
        public List<string>? SelectedDocumentData { get; set; } = new();
        public List<string>? DocumentFileNames { get; set; } = new();

        // Step 5: Nhà tài trợ (Không bắt buộc)
        public List<SponsorCreateDto> Sponsors { get; set; } = new();

        // Xác nhận
        public bool Confirmed { get; set; } = false;
    }

    public class RegistrationRoundCreateDto
    {
        public string RoundName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class CompetitionRoundCreateDto
    {
        public string RoundName { get; set; } = string.Empty;    // "Sơ khảo", "Bán kết", "Chung kết"
        public int RoundOrder { get; set; } = 1;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int? MaxAdvancing { get; set; }                   // Số đội/thí sinh được nhận vào vòng này
    }

    public class ScoringCriteriaCreateDto
    {
        public string CriteriaName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal MaxScore { get; set; } = 100;
        public decimal Weight { get; set; } = 1.0m;
        public int Order { get; set; } = 0;
    }

    public class SponsorCreateDto
    {
        [Required(ErrorMessage = "Tên nhà tài trợ là bắt buộc.")]
        public string SponsorName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Website { get; set; }
        public string? LogoUrl { get; set; }
        public string? Description { get; set; }
        public string SponsorshipLevel { get; set; } = "General"; // Gold, Silver, Bronze, General
        public decimal ContributionAmount { get; set; } = 0;
        public string Currency { get; set; } = "VND";
        public string? Notes { get; set; }
        public bool IsDisplayed { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;
    }

    // ─── Read-only DTOs for Edit page ───────────────────────────────────────

    public class ImageReadDto
    {
        public int ImageId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsThumbnail { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class DocumentReadDto
    {
        public int DocumentId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
    }

    public class CompetitionSponsorReadDto
    {
        public int CompetitionSponsorId { get; set; }
        public int SponsorId { get; set; }
        public string SponsorName { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public string SponsorshipLevel { get; set; } = "General";
        public decimal ContributionAmount { get; set; }
        public string Currency { get; set; } = "VND";
        public string? Notes { get; set; }
        public bool IsDisplayed { get; set; } = true;
        public int DisplayOrder { get; set; }
    }

    public class SponsorSearchDto
    {
        public int SponsorId { get; set; }
        public string SponsorName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? LogoUrl { get; set; }
    }

    public class AddSponsorToCompetitionDto
    {
        public int SponsorId { get; set; }
        public string SponsorshipLevel { get; set; } = "General";
        public decimal ContributionAmount { get; set; } = 0;
        public string Currency { get; set; } = "VND";
        public string? Notes { get; set; }
        public bool IsDisplayed { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;
    }

    // ─── Edit ViewModel ──────────────────────────────────────────────────────

    public class EditCompetitionViewModel : CreateCompetitionViewModel
    {
        public int CompetitionId { get; set; }

        // Trạng thái (readonly)
        public int RegistrationCount { get; set; }
        public int SubmissionCount { get; set; }
        public bool HasStarted { get; set; }
        public bool HasSubmissions { get; set; }

        // Rounds hiện có (readonly, không sửa trực tiếp)
        public List<RegistrationRoundReadDto> ExistingRounds { get; set; } = new();
        public RegistrationRoundCreateDto? NewRound { get; set; }

        // Vòng thi hiện có (readonly, quản lý qua AJAX)
        public List<CompetitionRoundReadDto> ExistingCompetitionRounds { get; set; } = new();

        // Ảnh, tài liệu, nhà tài trợ hiện có (readonly, quản lý qua AJAX)
        public List<ImageReadDto> Images { get; set; } = new();
        public List<DocumentReadDto> Documents { get; set; } = new();
        public List<CompetitionSponsorReadDto> ExistingCompetitionSponsors { get; set; } = new();
    }

    public class RegistrationRoundReadDto
    {
        public int RoundId { get; set; }
        public string RoundName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int RegistrationCount { get; set; }
        public bool HasRegistrations => RegistrationCount > 0;
        public bool IsActive => DateTime.Now >= StartDate && DateTime.Now <= EndDate;
        public bool IsPast => DateTime.Now > EndDate;
    }

    public class CompetitionRoundReadDto
    {
        public int RoundId { get; set; }
        public string RoundName { get; set; } = string.Empty;
        public int RoundOrder { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int? MaxAdvancing { get; set; }
        public string Status { get; set; } = "Upcoming";
        public bool IsActive => DateTime.UtcNow >= StartDate && DateTime.UtcNow <= EndDate;
        public bool IsPast => DateTime.UtcNow > EndDate;
    }

    public class UpdateCompetitionRoundDto
    {
        public string RoundName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int? MaxAdvancing { get; set; }
    }
}
