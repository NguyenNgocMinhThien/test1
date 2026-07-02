using Web_cham_diem.Models.ViewModels;

namespace Web_cham_diem.Services;

public interface IAuditLogService
{
    /// <summary>Ghi một bản ghi nhật ký hoạt động mới.</summary>
    Task LogAsync(
        int? userId,
        string? userEmail,
        string? userRole,
        string actionType,
        string module,
        int? targetId,
        string? description,
        string ipAddress,
        bool isSuccess,
        string? statusDetail = null);

    /// <summary>Lấy danh sách log có lọc + phân trang cho trang quản trị.</summary>
    Task<ActivityLogsViewModel> GetActivityLogsAsync(AuditLogFilter filter);
}