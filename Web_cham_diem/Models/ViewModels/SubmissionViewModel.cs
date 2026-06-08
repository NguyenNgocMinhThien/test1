using Microsoft.AspNetCore.Http;

namespace Web_cham_diem.Models.ViewModels
{
    public class SubmissionViewModel
    {
        // Competition info
        public int CompetitionId { get; set; }
        public string CompetitionName { get; set; } = string.Empty;
        public string? Category { get; set; }
        public DateTime SubmissionDeadline { get; set; }
        public bool IsTeamBased { get; set; }

        // Registration info
        public int RegistrationId { get; set; }
        public string RegistrationStatus { get; set; } = string.Empty;
        public string? TeamName { get; set; }
        public int? TeamId { get; set; }
        public List<SubmissionTeamMemberDto> TeamMembers { get; set; } = new();
        public List<SubmissionRoundDto> AvailableRounds { get; set; } = new();

        // Existing submission
        public int? ExistingSubmissionId { get; set; }
        public string? ExistingStatus { get; set; }
        public DateTime? ExistingSubmissionDate { get; set; }
        public string? ExistingFileUrl { get; set; }

        // Form fields
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? SelectedRoundId { get; set; }
        public IFormFile? SubmissionFile { get; set; }
        public string? VideoUrl { get; set; }
        public string? ProjectLink { get; set; }
        public string SubmitAction { get; set; } = "draft";

        // Current user
        public string UserFullName { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;

        public bool IsDeadlinePassed => DateTime.UtcNow > SubmissionDeadline;
        public bool CanSubmit => RegistrationStatus == "Approved" && !IsDeadlinePassed;
        public bool HasExistingSubmission => ExistingSubmissionId.HasValue;
        public bool CanEdit => CanSubmit && ExistingStatus != "Under Review" && ExistingStatus != "Evaluated";
    }

    public class SubmissionTeamMemberDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? StudentId { get; set; }
        public string Role { get; set; } = string.Empty;
    }

    public class SubmissionRoundDto
    {
        public int RoundId { get; set; }
        public string RoundName { get; set; } = string.Empty;
        public DateTime? SubmissionDeadline { get; set; }
        public string Status { get; set; } = string.Empty;
        public int RoundOrder { get; set; }
    }
}
