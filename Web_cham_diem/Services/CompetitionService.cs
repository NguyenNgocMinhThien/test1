using Web_cham_diem.Models;
using Web_cham_diem.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;

namespace Web_cham_diem.Services;

public class CompetitionService : ICompetitionService
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;

    public CompetitionService(ApplicationDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
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
            .Include(c => c.CompetitionImages)
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
            MaxTeamSize = c.MaxTeamSize,
            ProgressPercentage = CalculateProgress(c),
            CurrentPhase = DetermineCurrentPhase(c),
            StatusDisplay = GetStatusDisplay(c),
            IsTeamBased = c.IsTeamBased,
            // Lấy ảnh thumbnail hoặc ảnh đầu tiên
            BannerImageUrl = c.CompetitionImages?
                .FirstOrDefault(img => img.IsThumbnail)?
                .ImageUrl ?? c.CompetitionImages?
                .FirstOrDefault()?
                .ImageUrl
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
            .Include(c => c.Registrations)
            .Include(c => c.Submissions)
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
            IsTeamBased = competition.IsTeamBased,
            MaxParticipants = competition.MaxParticipants,
            MaxTeamSize = competition.MaxTeamSize,
            TotalRegistrations = competition.Registrations.Count,
            ApprovedRegistrations = competition.Registrations.Count(r => r.Status == "Approved"),
            TotalSubmissions = competition.Submissions.Count,
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

        // Tạo nhà tài trợ (nếu có)
        if (model.Sponsors != null && model.Sponsors.Any())
        {
            foreach (var sponsorDto in model.Sponsors.Where(s => !string.IsNullOrWhiteSpace(s.SponsorName)))
            {
                var sponsor = new Sponsors
                {
                    SponsorName = sponsorDto.SponsorName,
                    Email = sponsorDto.Email,
                    PhoneNumber = sponsorDto.PhoneNumber,
                    Website = sponsorDto.Website,
                    LogoUrl = sponsorDto.LogoUrl,
                    Description = sponsorDto.Description,
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow
                };
                _context.Sponsors.Add(sponsor);
                await _context.SaveChangesAsync();

                _context.CompetitionSponsors.Add(new CompetitionSponsors
                {
                    CompetitionId = competition.CompetitionId,
                    SponsorId = sponsor.SponsorId,
                    SponsorshipLevel = sponsorDto.SponsorshipLevel,
                    ContributionAmount = sponsorDto.ContributionAmount,
                    Currency = sponsorDto.Currency,
                    Notes = sponsorDto.Notes,
                    IsDisplayed = sponsorDto.IsDisplayed,
                    DisplayOrder = sponsorDto.DisplayOrder,
                    SponsoredAt = DateTime.UtcNow
                });
            }
            await _context.SaveChangesAsync();
        }

        await SaveCompetitionFilesAsync(competition.CompetitionId, model);

        return competition.CompetitionId;
    }

    private async Task SaveCompetitionFilesAsync(int competitionId, CreateCompetitionViewModel model)
    {
        // === LƯU ẢNH ===
        if (model.SelectedImageData != null && model.SelectedImageData.Any())
        {
            var imageDir = Path.Combine(_env.WebRootPath, "images", "competitions", competitionId.ToString());
            Directory.CreateDirectory(imageDir);
            bool isFirst = true;

            foreach (var dataUrl in model.SelectedImageData.Where(d => !string.IsNullOrWhiteSpace(d)))
            {
                try
                {
                    var commaIdx = dataUrl.IndexOf(',');
                    if (commaIdx < 0) continue;

                    var header = dataUrl[..commaIdx]; // "data:image/png;base64"
                    var base64 = dataUrl[(commaIdx + 1)..];

                    var ext = "jpg";
                    if (header.Contains('/'))
                    {
                        var mime = header.Split('/')[1].Split(';')[0].ToLower();
                        ext = mime == "jpeg" ? "jpg" : (mime.Length <= 5 ? mime : "jpg");
                    }

                    var fileName = $"{Guid.NewGuid()}.{ext}";
                    var filePath = Path.Combine(imageDir, fileName);
                    await File.WriteAllBytesAsync(filePath, Convert.FromBase64String(base64));

                    _context.CompetitionImages.Add(new CompetitionImages
                    {
                        CompetitionId = competitionId,
                        ImageUrl = $"/images/competitions/{competitionId}/{fileName}",
                        IsThumbnail = isFirst,
                        CreatedAt = DateTime.UtcNow
                    });
                    isFirst = false;
                }
                catch { /* bỏ qua file bị lỗi */ }
            }
            await _context.SaveChangesAsync();
        }

        // === LƯU TÀI LIỆU ===
        if (model.SelectedDocumentData != null && model.SelectedDocumentData.Any())
        {
            var docDir = Path.Combine(_env.WebRootPath, "pdf_excel", "competitions", competitionId.ToString());
            Directory.CreateDirectory(docDir);
            var fileNames = model.DocumentFileNames ?? new List<string>();

            for (int i = 0; i < model.SelectedDocumentData.Count; i++)
            {
                var dataUrl = model.SelectedDocumentData[i];
                if (string.IsNullOrWhiteSpace(dataUrl)) continue;

                try
                {
                    var commaIdx = dataUrl.IndexOf(',');
                    if (commaIdx < 0) continue;

                    var base64 = dataUrl[(commaIdx + 1)..];
                    var originalName = i < fileNames.Count ? fileNames[i] : $"document_{i + 1}";
                    var ext = Path.GetExtension(originalName).TrimStart('.');
                    if (string.IsNullOrEmpty(ext)) ext = "pdf";

                    var fileName = $"{Guid.NewGuid()}.{ext}";
                    var filePath = Path.Combine(docDir, fileName);
                    await File.WriteAllBytesAsync(filePath, Convert.FromBase64String(base64));

                    _context.CompetitionDocuments.Add(new CompetitionDocuments
                    {
                        CompetitionId = competitionId,
                        FileName = originalName,
                        FileUrl = $"/pdf_excel/competitions/{competitionId}/{fileName}",
                        FileType = ext.ToLower(),
                        UploadedAt = DateTime.UtcNow
                    });
                }
                catch { /* bỏ qua file bị lỗi */ }
            }
            await _context.SaveChangesAsync();
        }
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
            "Active" when now < competition.RegistrationDeadline => "Đang Nhận Hồ Sơ",
            "Active" when now < competition.SubmissionDeadline   => "Đang Thu Bài Thi",
            "Active"                                             => "Đang Chấm Điểm",
            "Draft"     => "Sắp Diễn Ra",
            "Closed"    => "Đã Đóng",
            "Completed" => "Đã Kết Thúc",
            _ => competition.Status
        };
    }

    // ===== DASHBOARD DATA =====
    public async Task<OrganizerDashboardViewModel> GetOrganizerDashboardDataAsync()
    {
        var now = DateTime.UtcNow;

        // Lấy tất cả cuộc thi với dữ liệu liên quan
        var competitions = await _context.Competitions
            .Include(c => c.Registrations)
            .Include(c => c.Submissions)
            .Include(c => c.ScoringCriteria)
            .ToListAsync();

        // Lấy tất cả giám khảo (Judges)
        var judges = await _context.Judges
            .ToListAsync();

        // 1. STATISTICS
        var activeCompetitions = competitions.Count(c => c.Status == "Active");
        var pendingRegistrations = await _context.Registrations
            .CountAsync(r => r.Status == "Pending");
        var totalSubmissions = competitions.Sum(c => c.Submissions.Count);
        var evaluatedSubmissions = await _context.Submissions
            .CountAsync(s => s.Status == "Evaluated");
        var activeJudges = judges.Count;
        var urgentCompetitions = competitions.Count(c => 
            c.Status == "Active" && (c.EndDate - now).TotalDays < 7 && (c.EndDate - now).TotalDays >= 0);

        // 2. PROGRESS DATA (cho biểu đồ)
        var progressData = new List<CompetitionProgressData>();
        var activeCompetition = competitions.FirstOrDefault(c => c.Status == "Active");
        if (activeCompetition != null)
        {
            // Tạo dữ liệu cho 4 tuần gần đây
            for (int i = 3; i >= 0; i--)
            {
                var weekStart = now.AddDays(-7 * i);
                var weekEnd = weekStart.AddDays(7);

                var registrationsInWeek = await _context.Registrations
                    .CountAsync(r => r.CompetitionId == activeCompetition.CompetitionId 
                        && r.RegistrationDate >= weekStart && r.RegistrationDate < weekEnd);

                var submissionsInWeek = await _context.Submissions
                    .CountAsync(s => s.CompetitionId == activeCompetition.CompetitionId 
                        && s.SubmissionDate >= weekStart && s.SubmissionDate < weekEnd);

                progressData.Add(new CompetitionProgressData
                {
                    Week = $"Tuần {i + 1}",
                    Registrations = registrationsInWeek,
                    Submissions = submissionsInWeek
                });
            }
        }

        // 3. APPROVAL RATIO DATA
        var allRegistrations = await _context.Registrations.ToListAsync();
        var approvalRatio = new ApprovalRatioData
        {
            ApprovedCount = allRegistrations.Count(r => r.Status == "Approved"),
            PendingCount = allRegistrations.Count(r => r.Status == "Pending"),
            RejectedCount = allRegistrations.Count(r => r.Status == "Rejected")
        };

        // 4. DEADLINES
        var deadlines = new List<DeadlineItem>();
        foreach (var comp in competitions.Where(c => c.Status == "Active" || c.Status == "Draft"))
        {
            // Deadline đóng cổng đăng ký
            if (comp.RegistrationDeadline > now)
            {
                var daysUntil = (comp.RegistrationDeadline - now).TotalDays;
                var status = daysUntil < 3 ? "urgent" : daysUntil < 7 ? "warning" : "normal";

                deadlines.Add(new DeadlineItem
                {
                    CompetitionId = comp.CompetitionId,
                    CompetitionName = comp.CompetitionName,
                    Title = "Đóng cổng Nhận hồ sơ",
                    DeadlineDate = comp.RegistrationDeadline,
                    Status = status,
                    Description = $"Cuộc thi: {comp.CompetitionName}. Đã nhận {comp.Registrations.Count} hồ sơ."
                });
            }

            // Deadline nộp bài
            if (comp.SubmissionDeadline > now)
            {
                var daysUntil = (comp.SubmissionDeadline - now).TotalDays;
                var status = daysUntil < 3 ? "urgent" : daysUntil < 7 ? "warning" : "normal";

                deadlines.Add(new DeadlineItem
                {
                    CompetitionId = comp.CompetitionId,
                    CompetitionName = comp.CompetitionName,
                    Title = "Đóng cổng Nhận bài dự thi",
                    DeadlineDate = comp.SubmissionDeadline,
                    Status = status,
                    Description = $"Cuộc thi: {comp.CompetitionName}. Hiện đã nhận {comp.Submissions.Count} bài thi."
                });
            }

            // Deadline chấm điểm (giả sử là ngày cuối cùng của cuộc thi)
            if (comp.EndDate > now && comp.Submissions.Count > 0)
            {
                var evaluatedCount = comp.Submissions.Count(s => s.Status == "Evaluated");
                var progressPercent = comp.Submissions.Count > 0 
                    ? (decimal)evaluatedCount / comp.Submissions.Count * 100 
                    : 0;

                var daysUntil = (comp.EndDate - now).TotalDays;
                var status = daysUntil < 3 ? "urgent" : daysUntil < 7 ? "warning" : "normal";

                deadlines.Add(new DeadlineItem
                {
                    CompetitionId = comp.CompetitionId,
                    CompetitionName = comp.CompetitionName,
                    Title = "Hạn nộp điểm của Giám khảo",
                    DeadlineDate = comp.EndDate,
                    Status = status,
                    ProgressPercentage = progressPercent,
                    Description = $"Cuộc thi: {comp.CompetitionName}."
                });
            }
        }

        // Sắp xếp deadlines theo thời gian
        deadlines = deadlines.OrderBy(d => d.DeadlineDate).ToList();

        // 5. RECENT ACTIVITIES (mô phỏng - cập nhật nếu có bảng ActivityLog thực tế)
        var recentActivities = new List<ActivityLog>();

        // Lấy các điểm được chấm gần đây
        var recentScores = await _context.Scores
            .Include(s => s.Submission)
            .OrderByDescending(s => s.ScoredDate)
            .Take(20)
            .ToListAsync();

        foreach (var score in recentScores.Take(5))
        {
            recentActivities.Add(new ActivityLog
            {
                Type = "score",
                Title = "Giám khảo chấm bài hoàn tất",
                Description = $"Bài thi #{score.SubmissionId} đã được chấm.",
                CreatedAt = score.ScoredDate,
                UserName = "Giám khảo"
            });
        }

        // Lấy các hồ sơ mới gần đây
        var recentRegistrations = await _context.Registrations
            .Include(r => r.User)
            .OrderByDescending(r => r.RegistrationDate)
            .Take(20)
            .ToListAsync();

        foreach (var reg in recentRegistrations.Where(r => r.Status == "Pending").Take(3))
        {
            recentActivities.Add(new ActivityLog
            {
                Type = "registration",
                Title = "Hồ sơ đăng ký mới chờ duyệt",
                Description = $"Sinh viên {reg.User?.FullName ?? "Unknown"} đã nộp hồ sơ.",
                CreatedAt = reg.RegistrationDate,
                UserName = reg.User?.FullName
            });
        }

        // Lấy các bài nộp gần đây
        var recentSubmissions = await _context.Submissions
            .OrderByDescending(s => s.SubmissionDate)
            .Take(20)
            .ToListAsync();

        foreach (var sub in recentSubmissions.Take(2))
        {
            recentActivities.Add(new ActivityLog
            {
                Type = "submission",
                Title = "Bài dự thi mới được nộp",
                Description = $"Bài thi #{sub.SubmissionId} đã được nộp.",
                CreatedAt = sub.SubmissionDate
            });
        }

        recentActivities = recentActivities.OrderByDescending(a => a.CreatedAt).ToList();

        // Xây dựng ViewModel
        return new OrganizerDashboardViewModel
        {
            ActiveCompetitions = activeCompetitions,
            PendingRegistrations = pendingRegistrations,
            TotalSubmissions = totalSubmissions,
            EvaluatedSubmissions = evaluatedSubmissions,
            ActiveJudges = activeJudges,
            UrgentCompetitions = urgentCompetitions,
            ProgressData = progressData,
            ApprovalRatio = approvalRatio,
            UpcomingDeadlines = deadlines,
            RecentActivities = recentActivities
        };
    }
}
