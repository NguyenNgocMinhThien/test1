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
        public DateTime StartDate { get; set; } = DateTime.UtcNow.AddDays(1);

        [Required]
        public DateTime EndDate { get; set; } = DateTime.UtcNow.AddDays(31);

        [Required]
        public DateTime RegistrationDeadline { get; set; } = DateTime.UtcNow.AddDays(10);

        [Required]
        public DateTime SubmissionDeadline { get; set; } = DateTime.UtcNow.AddDays(20);

        [Range(1, int.MaxValue, ErrorMessage = "Số lượng tham gia phải lớn hơn 0.")]
        public int MaxParticipants { get; set; } = 100;

        [Range(1, int.MaxValue, ErrorMessage = "Số người/đội phải lớn hơn 0.")]
        public int MaxTeamSize { get; set; } = 5;
        public bool IsTeamBased { get; set; } = false;

        // Step 3: Tiêu chí chấm điểm
        public List<ScoringCriteriaCreateDto> ScoringCriteria { get; set; } = new()
        {
            new ScoringCriteriaCreateDto { CriteriaName = "Ý tưởng", MaxScore = 100, Weight = 0.30m, Order = 1 },
            new ScoringCriteriaCreateDto { CriteriaName = "Thực hiện", MaxScore = 100, Weight = 0.40m, Order = 2 },
            new ScoringCriteriaCreateDto { CriteriaName = "Thuyết trình", MaxScore = 100, Weight = 0.30m, Order = 3 }
        };

        // Step 5: Nhà tài trợ (Không bắt buộc)
        public List<SponsorCreateDto> Sponsors { get; set; } = new();

        // Step 4: Ảnh & Tài liệu (Không bắt buộc)
        /// <summary>
        /// Danh sách ảnh được chọn (base64 hoặc URL từ form upload)
        /// Sẽ được xử lý để tải lên storage sau khi cuộc thi được tạo
        /// </summary>
        public List<string>? SelectedImageData { get; set; } = new();

        /// <summary>
        /// Danh sách tài liệu được chọn (base64 hoặc URL từ form upload)
        /// Sẽ được xử lý để tải lên storage sau khi cuộc thi được tạo
        /// </summary>
        public List<string>? SelectedDocumentData { get; set; } = new();

        /// <summary>
        /// Tên file tài liệu tương ứng (để sử dụng khi upload)
        /// </summary>
        public List<string>? DocumentFileNames { get; set; } = new();

        // Step 5: Xác nhận
        public bool Confirmed { get; set; } = false;
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

    public class EditCompetitionViewModel : CreateCompetitionViewModel
    {
        public int CompetitionId { get; set; }
    }
}