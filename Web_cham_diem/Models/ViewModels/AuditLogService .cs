using Microsoft.EntityFrameworkCore;
using Web_cham_diem.Models;
using Web_cham_diem.Models.ViewModels;

namespace Web_cham_diem.Services;

public class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuditLogService> _logger;

    // Các module được xem là "nhạy cảm" khi bị DELETE -> cần cảnh báo
    private static readonly string[] SensitiveDeleteModules =
        { "Contests", "Submissions", "Registrations", "UserManagement", "Sponsors" };

    // Các module cấu hình hệ thống -> UPDATE cần cảnh báo theo dõi
    private static readonly string[] ConfigModules = { "SystemSettings", "UserManagement" };

    public AuditLogService(ApplicationDbContext context, ILogger<AuditLogService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogAsync(
    int? userId,
    string? userEmail,
    string? userRole,
    string actionType,
    string module,
    int? targetId,
    string? description,
    string ipAddress,
    bool isSuccess,
    string? statusDetail = null)
    {
        try
        {
            var log = new AuditLogs
            {
                UserId = userId,
                UserEmailSnapshot = string.IsNullOrWhiteSpace(userEmail) ? "unknown" : userEmail.Trim(),
                UserRoleSnapshot = string.IsNullOrWhiteSpace(userRole) ? "-" : userRole.Trim(),
                ActionType = actionType,
                Module = module,
                TargetId = targetId,
                Description = description ?? $"{actionType} on {module}",
                IpAddress = string.IsNullOrWhiteSpace(ipAddress) ? "unknown" : ipAddress,
                IsSuccess = isSuccess,
                StatusDetail = statusDetail ?? (isSuccess ? "Success" : "Failed"),
                CreatedAt = DateTime.UtcNow
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Không thể ghi Audit Log: {Action} - {Module} - User: {Email}",
                actionType, module, userEmail);
            // Thử ghi log hệ thống để debug
            Console.WriteLine($"AUDIT ERROR: {actionType} {module} - {ex.Message}");
        }
    }

    public async Task<ActivityLogsViewModel> GetActivityLogsAsync(AuditLogFilter filter)
    {
        var query = _context.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim().ToLower();
            query = query.Where(a =>
                a.UserEmailSnapshot.ToLower().Contains(s) ||
                a.IpAddress.ToLower().Contains(s) ||
                (a.UserId != null && a.UserId.ToString() == s));
        }

        if (!string.IsNullOrWhiteSpace(filter.Module))
        {
            query = query.Where(a => a.Module == filter.Module);
        }

        if (!string.IsNullOrWhiteSpace(filter.ActionType))
        {
            query = query.Where(a => a.ActionType == filter.ActionType);
        }

        if (filter.Date.HasValue)
        {
            var from = filter.Date.Value.Date;
            var to = from.AddDays(1);
            query = query.Where(a => a.CreatedAt >= from && a.CreatedAt < to);
        }

        var totalRecords = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalRecords / (double)filter.PageSize);
        if (filter.Page < 1) filter.Page = 1;

        var logs = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(a => new AuditLogRowViewModel
            {
                LogId = a.LogId,
                CreatedAt = a.CreatedAt,
                UserEmailSnapshot = a.UserEmailSnapshot,
                UserRoleSnapshot = a.UserRoleSnapshot,
                ActionType = a.ActionType,
                Module = a.Module,
                TargetId = a.TargetId,
                Description = a.Description,
                IpAddress = a.IpAddress,
                IsSuccess = a.IsSuccess,
                StatusDetail = a.StatusDetail
            })
            .ToListAsync();

        var stats = await BuildStatsAsync();
        var alerts = await BuildSecurityAlertsAsync();

        return new ActivityLogsViewModel
        {
            Filter = filter,
            Logs = logs,
            TotalRecords = totalRecords,
            TotalPages = totalPages,
            Stats = stats,
            Alerts = alerts
        };
    }

    private async Task<AuditLogStatsViewModel> BuildStatsAsync()
    {
        var now = DateTime.UtcNow;
        var last24h = now.AddHours(-24);
        var todayStart = now.Date;

        var logs24h = await _context.AuditLogs.CountAsync(a => a.CreatedAt >= last24h);

        var loginsToday = await _context.AuditLogs.CountAsync(a =>
            a.ActionType == "LOGIN" && a.IsSuccess && a.CreatedAt >= todayStart);

        var suspiciousCount = await _context.AuditLogs.CountAsync(a =>
            a.ActionType == "LOGIN" && !a.IsSuccess && a.CreatedAt >= last24h);

        var failedActionsToday = await _context.AuditLogs.CountAsync(a =>
            !a.IsSuccess && a.CreatedAt >= todayStart);

        return new AuditLogStatsViewModel
        {
            Logs24h = logs24h,
            LoginsToday = loginsToday,
            SuspiciousCount = suspiciousCount,
            FailedActionsToday = failedActionsToday
        };
    }

    private async Task<List<SecurityAlertViewModel>> BuildSecurityAlertsAsync()
    {
        var alerts = new List<SecurityAlertViewModel>();
        var last24h = DateTime.UtcNow.AddHours(-24);

        // 1) Brute-force: nhiều lần đăng nhập thất bại từ cùng 1 IP trong 24h gần nhất
        var failedLoginsByIp = await _context.AuditLogs
            .AsNoTracking()
            .Where(a => a.ActionType == "LOGIN" && !a.IsSuccess && a.CreatedAt >= last24h)
            .GroupBy(a => a.IpAddress)
            .Select(g => new { Ip = g.Key, Count = g.Count(), LastAt = g.Max(x => x.CreatedAt) })
            .Where(g => g.Count >= 3)
            .OrderByDescending(g => g.LastAt)
            .Take(5)
            .ToListAsync();

        foreach (var g in failedLoginsByIp)
        {
            alerts.Add(new SecurityAlertViewModel
            {
                Severity = "danger",
                Title = "Brute Force Alert",
                Description = $"Cảnh báo {g.Count} lần đăng nhập thất bại từ IP {g.Ip}.",
                OccurredAt = g.LastAt
            });
        }

        // 2) Xóa dữ liệu nhạy cảm
        var sensitiveDeletes = await _context.AuditLogs
            .AsNoTracking()
            .Where(a => a.ActionType == "DELETE" && a.IsSuccess &&
                        a.CreatedAt >= last24h && SensitiveDeleteModules.Contains(a.Module))
            .OrderByDescending(a => a.CreatedAt)
            .Take(5)
            .ToListAsync();

        foreach (var d in sensitiveDeletes)
        {
            alerts.Add(new SecurityAlertViewModel
            {
                Severity = "warning",
                Title = "Xóa dữ liệu nhạy cảm",
                Description = $"Người dùng {d.UserEmailSnapshot} vừa thực hiện lệnh XÓA (Delete) trên {d.Module}" +
                               (d.TargetId.HasValue ? $" ID #{d.TargetId}." : "."),
                OccurredAt = d.CreatedAt
            });
        }

        // 3) Cập nhật cấu hình hệ thống / phân quyền bởi Admin
        var configUpdates = await _context.AuditLogs
            .AsNoTracking()
            .Where(a => a.ActionType == "UPDATE" && a.IsSuccess &&
                        a.CreatedAt >= last24h && ConfigModules.Contains(a.Module) &&
                        a.UserRoleSnapshot == "Admin")
            .OrderByDescending(a => a.CreatedAt)
            .Take(5)
            .ToListAsync();

        foreach (var c in configUpdates)
        {
            alerts.Add(new SecurityAlertViewModel
            {
                Severity = "info",
                Title = "Admin Config Update",
                Description = $"Cấu hình {c.Module} đã được sửa đổi bởi {c.UserEmailSnapshot}.",
                OccurredAt = c.CreatedAt
            });
        }

        return alerts.OrderByDescending(a => a.OccurredAt).Take(8).ToList();
    }
}