using Web_cham_diem.Models;
using Web_cham_diem.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Web_cham_diem.Services;

public class CompetitionService : ICompetitionService
{
    private readonly ApplicationDbContext _context;

    public CompetitionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Competitions>> GetAllCompetitionsAsync()
    {
        return await _context.Competitions
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<Competitions> GetCompetitionByIdAsync(int id)
    {
        return await _context.Competitions
            .Include(c => c.Registrations)
            .Include(c => c.Teams)
            .Include(c => c.ScoringCriteria)
            .FirstOrDefaultAsync(c => c.CompetitionId == id);
    }

    public async Task<List<Teams>> GetCompetitionTeamsAsync(int competitionId)
    {
        return await _context.Teams
            .Where(t => t.CompetitionId == competitionId)
            .Include(t => t.Leader)
            .ToListAsync();
    }

    public async Task<List<Registrations>> GetCompetitionRegistrationsAsync(int competitionId)
    {
        return await _context.Registrations
            .Where(r => r.CompetitionId == competitionId)
            .Include(r => r.User)
            .Include(r => r.Team)
            .ToListAsync();
    }

    public async Task<OrganizerContestsViewModel> GetOrganizerContestsAsync(string? searchQuery, string? statusFilter, string? categoryFilter, int pageNumber = 1)
    {
        var query = _context.Competitions.AsQueryable();

        // Áp dụng filters
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.Where(c => c.CompetitionName.Contains(searchQuery) || c.Category.Contains(searchQuery));
        }

        if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "all")
        {
            query = query.Where(c => c.Status == statusFilter);
        }

        if (!string.IsNullOrWhiteSpace(categoryFilter) && categoryFilter != "all")
        {
            query = query.Where(c => c.Category == categoryFilter);
        }

        // Lấy danh sách cuộc thi
        var competitions = await query
            .Include(c => c.Registrations)
            .Include(c => c.Submissions)
            .Include(c => c.ScoringCriteria)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        // Chuyển đổi thành DTO
        var competitionDtos = competitions.Select(c => new CompetitionOrganizerDto
        {
            CompetitionId = c.CompetitionId,
            CompetitionName = c.CompetitionName,
            Category = c.Category,
            Status = c.Status,
            StartDate = c.StartDate,
            EndDate = c.EndDate,
            RegistrationDeadline = c.RegistrationDeadline,
            SubmissionDeadline = c.SubmissionDeadline,
            TotalRegistrations = c.Registrations.Count,
            ApprovedRegistrations = c.Registrations.Count(r => r.Status == "Approved"),
            TotalSubmissions = c.Submissions.Count,
            EvaluatedSubmissions = c.Submissions.Count(s => s.Status == "Evaluated"),
            MaxParticipants = c.MaxParticipants,
            ProgressPercentage = CalculateProgress(c),
            CurrentPhase = DetermineCurrentPhase(c),
            StatusDisplay = GetStatusDisplay(c),
            IsTeamBased = c.IsTeamBased
        }).ToList();

        // Tính toán thống kê
        var viewModel = new OrganizerContestsViewModel
        {
            TotalCompetitions = competitions.Count,
            ActiveCompetitions = competitions.Count(c => c.Status == "Active"),
            UpcomingCompetitions = competitions.Count(c => c.Status == "Draft" || (c.Status == "Active" && c.StartDate > DateTime.UtcNow)),
            ClosedCompetitions = competitions.Count(c => c.Status == "Closed" || c.Status == "Completed"),
            Competitions = competitionDtos,
            SearchQuery = searchQuery,
            StatusFilter = statusFilter ?? "all",
            CategoryFilter = categoryFilter ?? "all",
            PageNumber = pageNumber
        };

        return viewModel;
    }

    public async Task<CompetitionDetailViewModel> GetCompetitionDetailAsync(int competitionId)
    {
        var competition = await _context.Competitions
            .Include(c => c.ScoringCriteria)
            .FirstOrDefaultAsync(c => c.CompetitionId == competitionId);

        if (competition == null)
            return null;

        var scoringCriteria = competition.ScoringCriteria.Select(sc => new ScoringCriteriaDto
        {
            CriteriaId = sc.CriteriaId,
            CriteriaName = sc.CriteriaName,
            Weight = sc.Weight,
            MaxScore = sc.MaxScore
        }).ToList();

        return new CompetitionDetailViewModel
        {
            CompetitionId = competition.CompetitionId,
            CompetitionName = competition.CompetitionName,
            Category = competition.Category,
            Description = competition.Description,
            Rules = competition.Rules,
            Prize = competition.Prize,
            StartDate = competition.StartDate,
            EndDate = competition.EndDate,
            RegistrationDeadline = competition.RegistrationDeadline,
            SubmissionDeadline = competition.SubmissionDeadline,
            Status = competition.Status,
            ScoringCriteria = scoringCriteria
        };
    }

    // ===== MỚI - CREATE COMPETITION =====
    private static DateTime ToUtc(DateTime value)
    {
        return DateTime.SpecifyKind(value, DateTimeKind.Local).ToUniversalTime();
    }

    public async Task<int> CreateCompetitionAsync(CreateCompetitionViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.CompetitionName))
            throw new InvalidOperationException("Tên cuộc thi không được để trống.");

        if (string.IsNullOrWhiteSpace(model.Category))
            throw new InvalidOperationException("Vui lòng chọn lĩnh vực.");

        var startDate = ToUtc(model.StartDate);
        var endDate = ToUtc(model.EndDate);
        var registrationDeadline = ToUtc(model.RegistrationDeadline);
        var submissionDeadline = ToUtc(model.SubmissionDeadline);

        if (endDate <= startDate)
            throw new InvalidOperationException("Ngày kết thúc phải sau ngày bắt đầu");

        if (submissionDeadline >= endDate)
            throw new InvalidOperationException("Hạn nộp bài phải trước ngày kết thúc");

        if (registrationDeadline >= submissionDeadline)
            throw new InvalidOperationException("Hạn đăng ký phải trước hạn nộp bài");

        if (model.ScoringCriteria == null || model.ScoringCriteria.Count == 0)
            throw new InvalidOperationException("Phải có ít nhất một tiêu chí chấm điểm");

        foreach (var criteria in model.ScoringCriteria)
        {
            if (criteria.Weight > 1m)
            {
                criteria.Weight = criteria.Weight > 100m
                    ? criteria.Weight / 10000m
                    : criteria.Weight / 100m;
            }
        }

        var totalWeight = model.ScoringCriteria.Sum(s => s.Weight);
        if (Math.Abs(totalWeight - 1.0m) > 0.01m)
            throw new InvalidOperationException($"Tổng trọng số phải bằng 100%, hiện tại là {totalWeight * 100:F1}%");

        var competition = new Competitions
        {
            CompetitionName = model.CompetitionName,
            Description = model.Description,
            Category = model.Category,
            Rules = model.Rules,
            Prize = model.Prize,
            StartDate = startDate,
            EndDate = endDate,
            RegistrationDeadline = registrationDeadline,
            SubmissionDeadline = submissionDeadline,
            MaxParticipants = model.MaxParticipants,
            MaxTeamSize = model.MaxTeamSize,
            IsTeamBased = model.IsTeamBased,
            Status = "Draft",
            CreatedAt = DateTime.UtcNow
        };

        _context.Competitions.Add(competition);
        await _context.SaveChangesAsync();

        foreach (var criteria in model.ScoringCriteria)
        {
            _context.ScoringCriteria.Add(new ScoringCriteria
            {
                CompetitionId = competition.CompetitionId,
                CriteriaName = criteria.CriteriaName,
                Description = criteria.Description,
                MaxScore = criteria.MaxScore,
                Weight = criteria.Weight,
                Order = criteria.Order,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        return competition.CompetitionId;
    }

    // ===== MỚI - GET FOR EDIT =====
    public async Task<EditCompetitionViewModel> GetCompetitionForEditAsync(int competitionId)
    {
        var competition = await _context.Competitions
            .Include(c => c.ScoringCriteria)
            .FirstOrDefaultAsync(c => c.CompetitionId == competitionId);

        if (competition == null)
            return null;

        var scoringCriteria = competition.ScoringCriteria
            .OrderBy(sc => sc.Order)
            .Select(sc => new ScoringCriteriaCreateDto
            {
                CriteriaName = sc.CriteriaName,
                Description = sc.Description,
                MaxScore = sc.MaxScore,
                Weight = sc.Weight,
                Order = sc.Order
            }).ToList();

        return new EditCompetitionViewModel
        {
            CompetitionId = competition.CompetitionId,
            CompetitionName = competition.CompetitionName,
            Description = competition.Description,
            Category = competition.Category,
            Rules = competition.Rules,
            Prize = competition.Prize,
            StartDate = competition.StartDate,
            EndDate = competition.EndDate,
            RegistrationDeadline = competition.RegistrationDeadline,
            SubmissionDeadline = competition.SubmissionDeadline,
            MaxParticipants = competition.MaxParticipants,
            MaxTeamSize = competition.MaxTeamSize,
            IsTeamBased = competition.IsTeamBased,
            ScoringCriteria = scoringCriteria
        };
    }

    // ===== MỚI - UPDATE COMPETITION =====
    public async Task<bool> UpdateCompetitionAsync(int competitionId, EditCompetitionViewModel model)
    {
        var competition = await _context.Competitions
            .Include(c => c.ScoringCriteria)
            .FirstOrDefaultAsync(c => c.CompetitionId == competitionId);

        if (competition == null)
            return false;

        // Không cho sửa nếu đã có người đăng ký
        var registrationCount = await _context.Registrations
            .CountAsync(r => r.CompetitionId == competitionId);

        if (competition.Status == "Active" && registrationCount > 0)
            throw new InvalidOperationException("Không thể sửa cuộc thi khi đã có người đăng ký");

        // Convert sang UTC trước
        var startDate = DateTime.SpecifyKind(model.StartDate, DateTimeKind.Local).ToUniversalTime();

        var endDate = DateTime.SpecifyKind(model.EndDate, DateTimeKind.Local).ToUniversalTime();

        var registrationDeadline = DateTime
            .SpecifyKind(model.RegistrationDeadline, DateTimeKind.Local)
            .ToUniversalTime();

        var submissionDeadline = DateTime
            .SpecifyKind(model.SubmissionDeadline, DateTimeKind.Local)
            .ToUniversalTime();

        // Validate
        if (endDate <= startDate)
            throw new InvalidOperationException("Ngày kết thúc phải sau ngày bắt đầu");

        if (submissionDeadline >= endDate)
            throw new InvalidOperationException("Hạn nộp bài phải trước ngày kết thúc");

        if (registrationDeadline >= submissionDeadline)
            throw new InvalidOperationException("Hạn đăng ký phải trước hạn nộp bài");

        if (model.ScoringCriteria == null || !model.ScoringCriteria.Any())
            throw new InvalidOperationException("Phải có ít nhất một tiêu chí chấm điểm");

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Update thông tin cuộc thi
            competition.CompetitionName = model.CompetitionName;
            competition.Description = model.Description;
            competition.Category = model.Category;
            competition.Rules = model.Rules;
            competition.Prize = model.Prize;

            // dùng UTC
            competition.StartDate = startDate;
            competition.EndDate = endDate;
            competition.RegistrationDeadline = registrationDeadline;
            competition.SubmissionDeadline = submissionDeadline;

            competition.MaxParticipants = model.MaxParticipants;
            competition.MaxTeamSize = model.MaxTeamSize;
            competition.IsTeamBased = model.IsTeamBased;
            competition.UpdatedAt = DateTime.UtcNow;

            // Xóa tiêu chí cũ
            _context.ScoringCriteria.RemoveRange(competition.ScoringCriteria);

            // Thêm tiêu chí mới
            foreach (var criteria in model.ScoringCriteria)
            {
                var scoringCriteria = new ScoringCriteria
                {
                    CompetitionId = competition.CompetitionId,
                    CriteriaName = criteria.CriteriaName,
                    Description = criteria.Description,
                    MaxScore = criteria.MaxScore,
                    Weight = criteria.Weight,
                    Order = criteria.Order,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ScoringCriteria.Add(scoringCriteria);
            }

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // ===== MỚI - DELETE COMPETITION =====
    public async Task<bool> DeleteCompetitionAsync(int competitionId)
    {
        var competition = await _context.Competitions
            .Include(c => c.Registrations)
            .Include(c => c.ScoringCriteria)
            .FirstOrDefaultAsync(c => c.CompetitionId == competitionId);

        if (competition == null)
            return false;

        // Không cho phép xóa nếu đã có người đăng ký
        if (competition.Registrations.Count > 0)
            throw new InvalidOperationException("Không thể xóa cuộc thi có người đăng ký");

        _context.ScoringCriteria.RemoveRange(competition.ScoringCriteria);
        _context.Competitions.Remove(competition);
        await _context.SaveChangesAsync();
        return true;
    }

    // ===== MỚI - CHANGE STATUS =====
    public async Task<bool> ChangeCompetitionStatusAsync(int competitionId, string newStatus)
    {
        var competition = await _context.Competitions
            .FirstOrDefaultAsync(c => c.CompetitionId == competitionId);

        if (competition == null)
            return false;

        // Validate trạng thái
        var validStatuses = new[] { "Draft", "Active", "Closed", "Completed" };
        if (!validStatuses.Contains(newStatus))
            throw new InvalidOperationException("Trạng thái không hợp lệ");

        competition.Status = newStatus;
        competition.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    // ===== HELPER METHODS =====
    private int CalculateProgress(Competitions competition)
    {
        var now = DateTime.UtcNow;
        var start = competition.StartDate;
        var end = competition.EndDate;

        if (now < start) return 0;
        if (now > end) return 100;

        var totalDuration = (end - start).TotalDays;
        var elapsed = (now - start).TotalDays;
        return (int)((elapsed / totalDuration) * 100);
    }

    private string DetermineCurrentPhase(Competitions competition)
    {
        var now = DateTime.UtcNow;

        if (now < competition.RegistrationDeadline)
            return "Duyệt hồ sơ mở màn";
        else if (now < competition.SubmissionDeadline)
            return "Thu bài dự thi";
        else if (now < competition.EndDate)
            return "Giám khảo chấm thi";
        else
            return "Công bố kết quả";
    }

    private string GetStatusDisplay(Competitions competition)
    {
        var now = DateTime.UtcNow;

        return competition.Status switch
        {
            "Active" when now < competition.RegistrationDeadline => "Đang Nhận Bài",
            "Active" when now < competition.SubmissionDeadline => "Đang Nhận Bài",
            "Active" => "Đang Chấm Điểm",
            "Draft" => "Sắp Diễn Ra",
            "Closed" => "Đã Đóng",
            "Completed" => "Đã Kết Thúc",
            _ => competition.Status
        };
    }
}