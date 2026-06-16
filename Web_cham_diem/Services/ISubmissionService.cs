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
    Task<bool> ApproveRegistrationAsync(int registrationId, int editorId, string? feedback = null);

    /// <summary>
    /// Từ chối hồ sơ đăng ký
    /// </summary>
    Task<bool> RejectRegistrationAsync(int registrationId, int editorId, string reason);

    /// <summary>
    /// Yêu cầu bổ sung thông tin
    /// </summary>
    Task<bool> RequestSupplementAsync(int registrationId, int editorId, string feedback);

    /// <summary>
    /// Lấy chi tiết hồ sơ
    /// </summary>
    Task<RegistrationDetailDto> GetRegistrationDetailAsync(int registrationId);

    /// <summary>
    /// Lấy lịch sử chỉnh sửa hồ sơ đăng ký
    /// </summary>
    Task<List<RegistrationHistoryItemDto>> GetRegistrationHistoryAsync(int registrationId);

    /// <summary>
    /// Tải file bài nộp
    /// </summary>
    Task<(byte[] fileBytes, string fileName)> DownloadSubmissionAsync(int submissionId);
}

public class RegistrationHistoryItemDto
{
    public int HistoryId { get; set; }
    public DateTime EditedAt { get; set; }
    public string EditorName { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string? ChangesSummary { get; set; }
    public string? PreviousStatus { get; set; }
    public string? NewStatus { get; set; }
}