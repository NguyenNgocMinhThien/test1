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
        int organizerId,
        int? competitionId = null,
        string? searchQuery = null,
        string? statusFilter = null,
        string? departmentFilter = null,
        string? registrationTypeFilter = null)
    {
        try
        {
            // 1. Lấy danh sách cuộc thi của organizer hiện tại
            var competitions = await _context.Competitions
                .Where(c => c.CreatedByUserId == organizerId)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CompetitionBasicDto
                {
                    CompetitionId = c.CompetitionId,
                    CompetitionName = c.CompetitionName
                })
                .ToListAsync();

            var ownedIds = competitions.Select(c => c.CompetitionId).ToList();

            // 2. Lấy thông tin cuộc thi được chọn (phải thuộc organizer)
            Competitions? selectedCompetition = null;
            if (competitionId.HasValue && ownedIds.Contains(competitionId.Value))
            {
                selectedCompetition = await _context.Competitions
                    .Include(c => c.Registrations)
                    .FirstOrDefaultAsync(c => c.CompetitionId == competitionId.Value);
            }

            // 3. Lấy dữ liệu hồ sơ đăng ký (chỉ trong phạm vi cuộc thi của organizer)
            var registrationsQuery = _context.Registrations
                .Include(r => r.User)
                .Include(r => r.Team)
                .Include(r => r.Competition)
                .Where(r => ownedIds.Contains(r.CompetitionId))
                .AsQueryable();

            if (competitionId.HasValue && ownedIds.Contains(competitionId.Value))
                registrationsQuery = registrationsQuery.Where(r => r.CompetitionId == competitionId.Value);

            // Tìm kiếm theo: tên cuộc thi, tên đội dự thi, tên bài dự thi (Submission.Title liên kết qua RegistrationId)
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                registrationsQuery = registrationsQuery.Where(r =>
                    r.Competition.CompetitionName.Contains(searchQuery) ||
                    (r.Team != null && r.Team.TeamName.Contains(searchQuery)) ||
                    _context.Submissions.Any(s => s.RegistrationId == r.RegistrationId && s.Title.Contains(searchQuery)));
            }

            if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "all")
                registrationsQuery = registrationsQuery.Where(r => r.Status == statusFilter);

            if (!string.IsNullOrWhiteSpace(registrationTypeFilter))
                registrationsQuery = registrationsQuery.Where(r => r.RegistrationType == registrationTypeFilter);

            var registrations = await registrationsQuery
                .OrderByDescending(r => r.RegistrationDate)
                .ToListAsync();

            // Lấy tiêu đề bài nộp liên kết với từng hồ sơ đăng ký (Registration → Submission.Title)
            var registrationIds = registrations.Select(r => r.RegistrationId).ToList();
            var submissionTitleRaw = await _context.Submissions
                .Where(s => s.RegistrationId.HasValue && registrationIds.Contains(s.RegistrationId.Value))
                .Select(s => new { RegistrationId = s.RegistrationId!.Value, s.Title })
                .ToListAsync();
            var submissionTitleLookup = submissionTitleRaw
                .GroupBy(x => x.RegistrationId)
                .ToDictionary(g => g.Key, g => g.First().Title);

            var registrationDtos = registrations.Select(r => new RegistrationDetailDto
            {
                RegistrationId = r.RegistrationId,
                RepresentativeName = r.User.FullName,
                Email = r.User.Email,
                StudentId = r.User.StudentId,
                Department = string.Empty,
                TeamName = r.Team?.TeamName ?? r.User.FullName,
                Topic = submissionTitleLookup.TryGetValue(r.RegistrationId, out var title)
                    ? title
                    : (r.Notes ?? "Chưa có đề tài"),
                SubmissionDocument = r.SubmissionDocument,
                Status = r.Status,
                RegistrationDate = r.RegistrationDate,
                ApprovalDate = r.ApprovalDate,
                Notes = r.Notes ?? string.Empty,
                RegistrationId_FK = r.RegistrationId
            }).ToList();

            // 4. Lấy dữ liệu bài nộp (chỉ trong phạm vi cuộc thi của organizer)
            var submissionsQuery = _context.Submissions
                .Include(s => s.Registration)
                    .ThenInclude(r => r!.User)
                .Include(s => s.Team)
                    .ThenInclude(t => t!.Leader)
                .Include(s => s.Competition)
                .Include(s => s.CompetitionRound)
                .Where(s => ownedIds.Contains(s.CompetitionId))
                .AsQueryable();

            if (competitionId.HasValue && ownedIds.Contains(competitionId.Value))
                submissionsQuery = submissionsQuery.Where(s => s.CompetitionId == competitionId.Value);

            // Tìm kiếm theo: tên cuộc thi, tên đội dự thi, tên bài dự thi
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                submissionsQuery = submissionsQuery.Where(s =>
                    s.Title.Contains(searchQuery) ||
                    (s.Competition != null && s.Competition.CompetitionName.Contains(searchQuery)) ||
                    (s.Team != null && s.Team.TeamName.Contains(searchQuery)));
            }

            var submissions = await submissionsQuery
                .OrderByDescending(s => s.SubmissionDate)
                .ToListAsync();

            var deadline = selectedCompetition?.SubmissionDeadline;
            var submissionDtos = submissions.Select(s => new SubmissionDetailDto
            {
                SubmissionId = s.SubmissionId,
                Title = s.Title,
                Description = s.Description,
                FileName = !string.IsNullOrEmpty(s.FileUrl)
                    ? Path.GetFileName(s.FileUrl)
                    : string.Empty,
                FileType = !string.IsNullOrEmpty(s.FileUrl)
                    ? (Path.GetExtension(s.FileUrl)?.TrimStart('.').ToLower() ?? "unknown")
                    : "unknown",
                FileSizeInMB = 0,
                RepresentativeName = s.Registration?.User.FullName ?? s.Team?.Leader.FullName ?? "Unknown",
                TeamName = s.Team?.TeamName ?? s.Registration?.User.FullName ?? "Unknown",
                CompetitionName = s.Competition?.CompetitionName ?? string.Empty,
                RoundName = s.CompetitionRound?.RoundName,
                SubmissionDate = s.SubmissionDate,
                IsLate = deadline.HasValue && s.SubmissionDate > deadline.Value,
                Status = s.Status,
                FileUrl = s.FileUrl,
                VideoUrl = s.VideoUrl,
                ProjectLink = s.ProjectLink
            }).ToList();

            // 5. Tính toán thống kê tiến độ
            var progressStats = new ProgressStatisticsDto();
            if (selectedCompetition != null)
            {
                var now = DateTime.UtcNow;
                var approvedCount = selectedCompetition.Registrations.Count(r => r.Status == "Approved");
                var onTimeCount = deadline.HasValue
                    ? submissions.Count(s => s.SubmissionDate <= deadline.Value)
                    : submissions.Count;
                var lateCount = deadline.HasValue
                    ? submissions.Count(s => s.SubmissionDate > deadline.Value)
                    : 0;

                progressStats = new ProgressStatisticsDto
                {
                    TotalExpected = approvedCount,
                    OnTimeSubmissions = onTimeCount,
                    LateSubmissions = lateCount,
                    NotSubmitted = Math.Max(0, approvedCount - submissions.Count),
                    DeadlineDate = selectedCompetition.SubmissionDeadline,
                    HoursUntilDeadline = (int)(selectedCompetition.SubmissionDeadline - now).TotalHours,
                    IsDeadlinePassing = now > selectedCompetition.SubmissionDeadline
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
                RegistrationTypeFilter = registrationTypeFilter,
                Competitions = competitions
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting submissions view");
            throw;
        }
    }

    public async Task<bool> ApproveRegistrationAsync(int registrationId, int editorId, string? feedback = null)
    {
        var registration = await _context.Registrations.FindAsync(registrationId);
        if (registration == null)
            return false;

        var previousStatus = registration.Status;
        registration.Status = "Approved";
        registration.ApprovalDate = DateTime.UtcNow;
        registration.UpdatedAt = DateTime.UtcNow;
        registration.Notes = feedback ?? registration.Notes;

        _context.Registrations.Update(registration);
        _context.RegistrationEditHistories.Add(new Models.RegistrationEditHistory
        {
            RegistrationId = registrationId,
            EditedBy = editorId,
            EditedAt = DateTime.UtcNow,
            ActionType = "OrganizerApproved",
            ChangesSummary = string.IsNullOrWhiteSpace(feedback) ? "Duyệt hồ sơ đăng ký" : $"Duyệt hồ sơ. Nhận xét: {feedback}",
            PreviousStatus = previousStatus,
            NewStatus = "Approved"
        });
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Registration {registrationId} approved");
        return true;
    }

    public async Task<bool> RejectRegistrationAsync(int registrationId, int editorId, string reason)
    {
        var registration = await _context.Registrations.FindAsync(registrationId);
        if (registration == null)
            return false;

        var previousStatus = registration.Status;
        registration.Status = "Rejected";
        registration.UpdatedAt = DateTime.UtcNow;
        registration.Notes = reason;

        _context.Registrations.Update(registration);
        _context.RegistrationEditHistories.Add(new Models.RegistrationEditHistory
        {
            RegistrationId = registrationId,
            EditedBy = editorId,
            EditedAt = DateTime.UtcNow,
            ActionType = "OrganizerRejected",
            ChangesSummary = $"Từ chối hồ sơ. Lý do: {reason}",
            PreviousStatus = previousStatus,
            NewStatus = "Rejected"
        });
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Registration {registrationId} rejected");
        return true;
    }

    public async Task<bool> RequestSupplementAsync(int registrationId, int editorId, string feedback)
    {
        var registration = await _context.Registrations.FindAsync(registrationId);
        if (registration == null)
            return false;

        var previousStatus = registration.Status;
        registration.Status = "Pending";
        registration.UpdatedAt = DateTime.UtcNow;
        registration.Notes = feedback;

        _context.Registrations.Update(registration);
        _context.RegistrationEditHistories.Add(new Models.RegistrationEditHistory
        {
            RegistrationId = registrationId,
            EditedBy = editorId,
            EditedAt = DateTime.UtcNow,
            ActionType = "OrganizerSupplement",
            ChangesSummary = $"Yêu cầu bổ sung thông tin: {feedback}",
            PreviousStatus = previousStatus,
            NewStatus = "Pending"
        });
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Supplement requested for registration {registrationId}");
        return true;
    }

    public async Task<List<RegistrationHistoryItemDto>> GetRegistrationHistoryAsync(int registrationId)
    {
        return await _context.RegistrationEditHistories
            .Where(h => h.RegistrationId == registrationId)
            .Include(h => h.Editor)
            .OrderByDescending(h => h.EditedAt)
            .Select(h => new RegistrationHistoryItemDto
            {
                HistoryId = h.HistoryId,
                EditedAt = h.EditedAt,
                EditorName = h.Editor.FullName,
                ActionType = h.ActionType,
                ChangesSummary = h.ChangesSummary,
                PreviousStatus = h.PreviousStatus,
                NewStatus = h.NewStatus
            })
            .ToListAsync();
    }

    public async Task<RegistrationDetailDto> GetRegistrationDetailAsync(int registrationId)
    {
        var registration = await _context.Registrations
            .Include(r => r.User)
            .Include(r => r.Team)
            .Include(r => r.Advisor)
            .Include(r => r.FieldValues)
                .ThenInclude(v => v.Field)
            .FirstOrDefaultAsync(r => r.RegistrationId == registrationId);

        if (registration == null)
            return null;

        var customValues = registration.FieldValues
            .Where(v => v.Field.IsActive)
            .OrderBy(v => v.Field.DisplayOrder)
            .Select(v => new FormFieldValueDto
            {
                FieldId    = v.FieldId,
                FieldLabel = v.Field.FieldLabel,
                FieldType  = v.Field.FieldType,
                Value      = v.Value,
                FileName   = v.FileName
            })
            .ToList();

        return new RegistrationDetailDto
        {
            RegistrationId     = registration.RegistrationId,
            RepresentativeName = registration.User.FullName,
            Email              = registration.User.Email,
            StudentId          = registration.User.StudentId,
            Department         = "Công nghệ Thông tin",
            TeamName           = registration.Team?.TeamName ?? registration.User.FullName,
            Topic              = registration.SubmissionDocument ?? "Không có thông tin",
            SubmissionDocument = registration.SubmissionDocument,
            Status             = registration.Status,
            RegistrationDate   = registration.RegistrationDate,
            ApprovalDate       = registration.ApprovalDate,
            Notes              = registration.Notes ?? string.Empty,
            RegistrationId_FK  = registration.RegistrationId,
            RegistrationType   = registration.RegistrationType,
            AdvisorName        = registration.Advisor?.FullName,
            CustomFieldValues  = customValues
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
