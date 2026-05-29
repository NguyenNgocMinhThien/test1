using Web_cham_diem.Models;
using Web_cham_diem.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Web_cham_diem.Services;

public class SubmissionService : ISubmissionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SubmissionService> _logger;

    public SubmissionService(ApplicationDbContext context, ILogger<SubmissionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<OrganizerSubmissionsViewModel> GetSubmissionsViewAsync(
        int? competitionId = null,
        string? searchQuery = null,
        string? statusFilter = null,
        string? departmentFilter = null)
    {
        try
        {
            // 1. Lấy danh sách cuộc thi
            var competitionsQuery = _context.Competitions
                .Where(c => c.Status == "Active")
                .OrderByDescending(c => c.CreatedAt);

            var competitions = await competitionsQuery
                .Select(c => new CompetitionBasicDto
                {
                    CompetitionId = c.CompetitionId,
                    CompetitionName = c.CompetitionName
                })
                .ToListAsync();

            // Nếu không chọn cuộc thi, lấy cuộc thi đầu tiên (nếu có)
            if (!competitionId.HasValue && competitions.Any())
            {
                competitionId = competitions.First().CompetitionId;
            }

            // 2. Lấy dữ liệu hồ sơ đăng ký
            var registrationsQuery = _context.Registrations
                .Include(r => r.User)
                .Include(r => r.Team)
                .Include(r => r.Competition)
                .AsQueryable();

            if (competitionId.HasValue)
            {
                registrationsQuery = registrationsQuery.Where(r => r.CompetitionId == competitionId.Value);
            }

            // Apply filters
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                registrationsQuery = registrationsQuery.Where(r =>
                    r.User.FullName.Contains(searchQuery) ||
                    r.Team.TeamName.Contains(searchQuery) ||
                    r.User.StudentId.Contains(searchQuery) ||
                    r.User.Email.Contains(searchQuery));
            }

            if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "all")
            {
                registrationsQuery = registrationsQuery.Where(r => r.Status == statusFilter);
            }

            var registrations = await registrationsQuery.ToListAsync();

            // Map to DTOs
            var registrationDtos = registrations.Select(r => new RegistrationDetailDto
            {
                RegistrationId = r.RegistrationId,
                RepresentativeName = r.User.FullName,
                Email = r.User.Email,
                StudentId = r.User.StudentId,
                Department = "Công nghệ Thông tin", // TODO: Lấy từ User profile
                TeamName = r.Team?.TeamName ?? r.User.FullName,
                Topic = r.SubmissionDocument ?? "Không có thông tin",
                SubmissionDocument = r.SubmissionDocument,
                Status = r.Status,
                RegistrationDate = r.RegistrationDate,
                ApprovalDate = r.ApprovalDate,
                Notes = r.Notes ?? string.Empty,
                RegistrationId_FK = r.RegistrationId
            }).ToList();

            // 3. Lấy dữ liệu bài nộp
            var submissionsQuery = _context.Submissions
                .Include(s => s.Registration)
                    .ThenInclude(r => r.User)
                .Include(s => s.Team)
                    .ThenInclude(t => t.Leader)
                .AsQueryable();

            if (competitionId.HasValue)
            {
                submissionsQuery = submissionsQuery.Where(s => s.CompetitionId == competitionId.Value);
            }

            var submissions = await submissionsQuery.ToListAsync();

            var submissionDtos = submissions.Select(s => new SubmissionDetailDto
            {
                SubmissionId = s.SubmissionId,
                FileName = Path.GetFileName(s.FileUrl) ?? "File",
                FileType = Path.GetExtension(s.FileUrl)?.TrimStart('.').ToLower() ?? "unknown",
                FileSizeInMB = 0, // TODO: Tính từ file thực tế
                RepresentativeName = s.Registration?.User.FullName ?? s.Team?.Leader.FullName ?? "Unknown",
                TeamName = s.Team?.TeamName ?? s.Registration?.User.FullName ?? "Unknown",
                SubmissionDate = s.SubmissionDate,
                IsLate = false, // TODO: So sánh với deadline
                Status = s.Status,
                FileUrl = s.FileUrl
            }).ToList();

            // 4. Tính toán thống kê tiến độ
            var competition = competitionId.HasValue
                ? await _context.Competitions.FirstOrDefaultAsync(c => c.CompetitionId == competitionId.Value)
                : null;

            var progressStats = new ProgressStatisticsDto();
            if (competition != null)
            {
                var now = DateTime.UtcNow;
                progressStats = new ProgressStatisticsDto
                {
                    TotalExpected = competition.Registrations.Count,
                    OnTimeSubmissions = submissions.Count(s => !s.SubmissionDate.Date.IsAfter(competition.SubmissionDeadline.Date)),
                    LateSubmissions = submissions.Count(s => s.SubmissionDate.Date.IsAfter(competition.SubmissionDeadline.Date)),
                    NotSubmitted = competition.Registrations.Count - submissions.Count,
                    DeadlineDate = competition.SubmissionDeadline,
                    HoursUntilDeadline = (int)(competition.SubmissionDeadline - now).TotalHours,
                    IsDeadlinePassing = now > competition.SubmissionDeadline
                };

                if (progressStats.TotalExpected > 0)
                {
                    progressStats.OnTimePercentage = (progressStats.OnTimeSubmissions * 100.0) / progressStats.TotalExpected;
                    progressStats.LatePercentage = (progressStats.LateSubmissions * 100.0) / progressStats.TotalExpected;
                    progressStats.NotSubmittedPercentage = (progressStats.NotSubmitted * 100.0) / progressStats.TotalExpected;
                }
            }

            return new OrganizerSubmissionsViewModel
            {
                PendingRegistrations = registrationDtos.Count(r => r.Status == "Pending"),
                ApprovedRegistrations = registrationDtos.Count(r => r.Status == "Approved"),
                TotalSubmissions = submissionDtos.Count,
                LateSubmissions = submissionDtos.Count(s => s.IsLate),
                PendingRegistrationsList = registrationDtos.Where(r => r.Status == "Pending").ToList(),
                AllRegistrationsList = registrationDtos,
                SubmissionsList = submissionDtos,
                ProgressStatistics = progressStats,
                SelectedCompetitionId = competitionId,
                SearchQuery = searchQuery,
                StatusFilter = statusFilter,
                DepartmentFilter = departmentFilter,
                Competitions = competitions
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting submissions view");
            throw;
        }
    }

    public async Task<bool> ApproveRegistrationAsync(int registrationId, string? feedback = null)
    {
        var registration = await _context.Registrations.FindAsync(registrationId);
        if (registration == null)
            return false;

        registration.Status = "Approved";
        registration.ApprovalDate = DateTime.UtcNow;
        registration.UpdatedAt = DateTime.UtcNow;
        registration.Notes = feedback ?? registration.Notes;

        _context.Registrations.Update(registration);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Registration {registrationId} approved");
        return true;
    }

    public async Task<bool> RejectRegistrationAsync(int registrationId, string reason)
    {
        var registration = await _context.Registrations.FindAsync(registrationId);
        if (registration == null)
            return false;

        registration.Status = "Rejected";
        registration.UpdatedAt = DateTime.UtcNow;
        registration.Notes = reason;

        _context.Registrations.Update(registration);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Registration {registrationId} rejected");
        return true;
    }

    public async Task<bool> RequestSupplementAsync(int registrationId, string feedback)
    {
        var registration = await _context.Registrations.FindAsync(registrationId);
        if (registration == null)
            return false;

        registration.Status = "Pending"; // Hoặc tạo status riêng "RequestSupplement"
        registration.UpdatedAt = DateTime.UtcNow;
        registration.Notes = feedback;

        _context.Registrations.Update(registration);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Supplement requested for registration {registrationId}");
        return true;
    }

    public async Task<RegistrationDetailDto> GetRegistrationDetailAsync(int registrationId)
    {
        var registration = await _context.Registrations
            .Include(r => r.User)
            .Include(r => r.Team)
            .FirstOrDefaultAsync(r => r.RegistrationId == registrationId);

        if (registration == null)
            return null;

        return new RegistrationDetailDto
        {
            RegistrationId = registration.RegistrationId,
            RepresentativeName = registration.User.FullName,
            Email = registration.User.Email,
            StudentId = registration.User.StudentId,
            Department = "Công nghệ Thông tin",
            TeamName = registration.Team?.TeamName ?? registration.User.FullName,
            Topic = registration.SubmissionDocument ?? "Không có thông tin",
            SubmissionDocument = registration.SubmissionDocument,
            Status = registration.Status,
            RegistrationDate = registration.RegistrationDate,
            ApprovalDate = registration.ApprovalDate,
            Notes = registration.Notes ?? string.Empty,
            RegistrationId_FK = registration.RegistrationId
        };
    }

    public async Task<(byte[] fileBytes, string fileName)> DownloadSubmissionAsync(int submissionId)
    {
        var submission = await _context.Submissions.FindAsync(submissionId);
        if (submission == null)
            throw new InvalidOperationException("Submission not found");

        // TODO: Implement file download from storage
        throw new NotImplementedException("File download not implemented yet");
    }
}

internal static class DateExtensions
{
    public static bool IsAfter(this DateTime date, DateTime other)
    {
        return date > other;
    }
}