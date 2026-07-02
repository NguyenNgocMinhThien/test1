using Web_cham_diem.Models.ViewModels;

namespace Web_cham_diem.Services;

public interface INotificationsService
{
    Task<OrganizerNotificationsViewModel> GetOrganizerViewAsync(int organizerId, int? competitionId);
    Task<(bool Ok, string Message, int Count)> SendNotificationAsync(SendNotificationRequest req, int organizerId);
    Task<int> GetUnreadCountAsync(int userId);
    Task<List<UserNotificationDto>> GetRecentForUserAsync(int userId, int take = 10);
    Task<UserNotificationsPageViewModel> GetUserNotificationsPageAsync(int userId, string? typeFilter, bool unreadOnly, int page, int pageSize);
    Task<bool> MarkReadAsync(int notificationId, int userId);
    Task<int> MarkAllReadAsync(int userId);

    // Public Announcements
    Task<List<PublicAnnouncementDto>> GetPublicAnnouncementsAsync(int page = 1, int pageSize = 20);
    Task<(int Total, List<PublicAnnouncementDto> Items)> GetPublicAnnouncementsPagedAsync(string? search, int page, int pageSize);
    Task<PublicAnnouncementDto?> GetAnnouncementByIdAsync(int id);
    Task<int> CreateAnnouncementAsync(Web_cham_diem.Models.PublicAnnouncements announcement);
}

public class UserNotificationDto
{
    public int NotificationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "Info";
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? RelatedEntity { get; set; }
    public int? RelatedEntityId { get; set; }
    public string IconClass { get; set; } = "bi-bell";
    public string TimeAgo { get; set; } = string.Empty;
    public string CategoryLabel { get; set; } = string.Empty;
    public string? CompetitionName { get; set; }
}

public class UserNotificationsPageViewModel
{
    public List<UserNotificationDto> Notifications { get; set; } = new();
    public int TotalCount { get; set; }
    public int UnreadCount { get; set; }
    public int InfoCount { get; set; }
    public int SuccessCount { get; set; }
    public int WarningCount { get; set; }
    public int ErrorCount { get; set; }
    public string? TypeFilter { get; set; }
    public bool UnreadOnly { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
