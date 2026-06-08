using Web_cham_diem.Models.ViewModels;

namespace Web_cham_diem.Services;

public interface ISubmissionService
{
    /// <summary>
    /// Lấy dữ liệu trang Submissions cho Organizer
    /// </summary>
    Task<OrganizerSubmissionsViewModel> GetSubmissionsViewAsync(
        int? competitionId = null,
        string? searchQuery = null,
        string? statusFilter = null,
        string? departmentFilter = null,
        string? registrationTypeFilter = null);

    /// <summary>
    /// Duyệt hồ sơ đăng ký
    /// </summary>
    Task<bool> ApproveRegistrationAsync(int registrationId, string? feedback = null);

    /// <summary>
    /// Từ chối hồ sơ đăng ký
    /// </summary>
    Task<bool> RejectRegistrationAsync(int registrationId, string reason);

    /// <summary>
    /// Yêu cầu bổ sung thông tin
    /// </summary>
    Task<bool> RequestSupplementAsync(int registrationId, string feedback);

    /// <summary>
    /// Lấy chi tiết hồ sơ
    /// </summary>
    Task<RegistrationDetailDto> GetRegistrationDetailAsync(int registrationId);

    /// <summary>
    /// Tải file bài nộp
    /// </summary>
    Task<(byte[] fileBytes, string fileName)> DownloadSubmissionAsync(int submissionId);
}