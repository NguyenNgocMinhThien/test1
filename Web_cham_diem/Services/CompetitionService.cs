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

    // ===== HELPERS =====

    private static DateTime ToUtc(DateTime value)
    {
        if (value.Kind == DateTimeKind.Utc) return value;
        return DateTime.SpecifyKind(value, DateTimeKind.Local).ToUniversalTime();
    }

    /// <summary>Hạn đăng ký thực tế = EndDate của round có EndDate lớn nhất.</summary>
    private static DateTime? GetLatestRegistrationDeadline(IEnumerable<RegistrationRounds> rounds)
    {
        if (!rounds.Any()) return null;
        return rounds.Max(r => r.EndDate);
    }

    private static bool HasActiveRound(IEnumerable<RegistrationRounds> rounds)
    {
        var now = DateTime.UtcNow;
        return rounds.Any(r => now >= r.StartDate && now <= r.EndDate);
    }

    private int CalculateProgress(Competitions competition)
    {
        var now = DateTime.UtcNow;
        if (now < competition.StartDate) return 0;
        if (now > competition.EndDate) return 100;
        var totalDuration = (competition.EndDate - competition.StartDate).TotalDays;
        var elapsed = (now - competition.StartDate).TotalDays;
        return (int)((elapsed / totalDuration) * 100);
    }

    private string DetermineCurrentPhase(Competitions competition)
    {
        var now = DateTime.UtcNow;
        var latestDeadline = GetLatestRegistrationDeadline(competition.RegistrationRounds);

        if (latestDeadline.HasValue && now <= latestDeadline.Value)
            return "Duyệt hồ sơ mở màn";
        if (now < competition.SubmissionDeadline)
            return "Thu bài dự thi";
        if (now < competition.EndDate)
            return "Giám khảo chấm thi";
        return "Công bố kết quả";
    }

    private string GetStatusDisplay(Competitions competition)
    {
        var now = DateTime.UtcNow;
        var latestDeadline = GetLatestRegistrationDeadline(competition.RegistrationRounds);

        return competition.Status switch
        {
            "Active" when latestDeadline.HasValue && now <= latestDeadline.Value => "Đang Nhận Hồ Sơ",
            "Active" when now < competition.SubmissionDeadline => "Đang Thu Bài Thi",
            "Active" => "Đang Chấm Điểm",
            "Draft"     => "Sắp Diễn Ra",
            "Closed"    => "Đã Đóng",
            "Completed" => "Đã Kết Thúc",
            _ => competition.Status
        };
    }

    // ===== VALIDATE REGISTRATION ROUNDS =====

    private void ValidateRounds(List<RegistrationRoundCreateDto> rounds, DateTime competitionStartDate)
    {
        if (rounds == null || rounds.Count == 0)
            throw new InvalidOperationException("Phải có ít nhất một đợt đăng ký.");

        for (int i = 0; i < rounds.Count; i++)
        {
            var r = rounds[i];
            if (string.IsNullOrWhiteSpace(r.RoundName))
                throw new InvalidOperationException($"Đợt {i + 1}: Tên đợt đăng ký không được để trống.");

            var start = ToUtc(r.StartDate);
            var end = ToUtc(r.EndDate);

            if (end <= start)
                throw new InvalidOperationException($"Đợt \"{r.RoundName}\": Ngày kết thúc phải sau ngày bắt đầu.");

            if (end > competitionStartDate)
                throw new InvalidOperationException(
                    $"Đợt \"{r.RoundName}\": Hạn đăng ký ({end:dd/MM/yyyy}) phải trước hoặc bằng ngày bắt đầu cuộc thi ({competitionStartDate:dd/MM/yyyy}).");
        }

        // Kiểm tra chồng chéo
        var sorted = rounds
            .Select(r => (start: ToUtc(r.StartDate), end: ToUtc(r.EndDate), name: r.RoundName))
            .OrderBy(r => r.start)
            .ToList();

        for (int i = 1; i < sorted.Count; i++)
        {
            if (sorted[i].start <= sorted[i - 1].end)
                throw new InvalidOperationException(
                    $"Đợt \"{sorted[i].name}\" bắt đầu ({sorted[i].start:dd/MM/yyyy}) trùng hoặc chồng chéo với đợt \"{sorted[i - 1].name}\" (kết thúc {sorted[i - 1].end:dd/MM/yyyy}). Các đợt không được chồng chéo.");
        }
    }

    // ===== READ =====

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
            .Include(c => c.RegistrationRounds)
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

    public async Task<OrganizerContestsViewModel> GetOrganizerContestsAsync(
        string? searchQuery, string? statusFilter, string? categoryFilter, int pageNumber = 1)
    {
        var query = _context.Competitions.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
            query = query.Where(c => c.CompetitionName.Contains(searchQuery) || c.Category.Contains(searchQuery));

        if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "all")
            query = query.Where(c => c.Status == statusFilter);

        if (!string.IsNullOrWhiteSpace(categoryFilter) && categoryFilter != "all")
            query = query.Where(c => c.Category == categoryFilter);

        var competitions = await query
            .Include(c => c.Registrations)
            .Include(c => c.Submissions)
            .Include(c => c.CompetitionImages)
            .Include(c => c.RegistrationRounds)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        var competitionDtos = competitions.Select(c => new CompetitionOrganizerDto
        {
            CompetitionId       = c.CompetitionId,
            CompetitionName     = c.CompetitionName,
            Category            = c.Category,
            Status              = c.Status,
            StartDate           = c.StartDate,
            EndDate             = c.EndDate,
            SubmissionDeadline  = c.SubmissionDeadline,
            LatestRegistrationDeadline = GetLatestRegistrationDeadline(c.RegistrationRounds),
            TotalRegistrations  = c.Registrations.Count,
            ApprovedRegistrations = c.Registrations.Count(r => r.Status == "Approved"),
            TotalSubmissions    = c.Submissions.Count,
            EvaluatedSubmissions = c.Submissions.Count(s => s.Status == "Evaluated"),
            MinParticipants     = c.MinParticipants,
            MaxParticipants     = c.MaxParticipants,
            MaxTeamSize         = c.MaxTeamSize,
            ProgressPercentage  = CalculateProgress(c),
            CurrentPhase        = DetermineCurrentPhase(c),
            StatusDisplay       = GetStatusDisplay(c),
            IsTeamBased         = c.IsTeamBased,
            HasActiveRound      = HasActiveRound(c.RegistrationRounds),
            BannerImageUrl = c.CompetitionImages?
                .FirstOrDefault(img => img.IsThumbnail)?.ImageUrl
                ?? c.CompetitionImages?.FirstOrDefault()?.ImageUrl
        }).ToList();

        return new OrganizerContestsViewModel
        {
            TotalCompetitions    = competitions.Count,
            ActiveCompetitions   = competitions.Count(c => c.Status == "Active"),
            UpcomingCompetitions = competitions.Count(c => c.Status == "Draft" || (c.Status == "Active" && c.StartDate > DateTime.UtcNow)),
            ClosedCompetitions   = competitions.Count(c => c.Status == "Closed" || c.Status == "Completed"),
            Competitions         = competitionDtos,
            SearchQuery          = searchQuery,
            StatusFilter         = statusFilter ?? "all",
            CategoryFilter       = categoryFilter ?? "all",
            PageNumber           = pageNumber
        };
    }

    public async Task<CompetitionDetailViewModel> GetCompetitionDetailAsync(int competitionId)
    {
        var competition = await _context.Competitions
            .Include(c => c.ScoringCriteria)
            .Include(c => c.Registrations)
            .Include(c => c.Submissions)
            .Include(c => c.RegistrationRounds)
            .FirstOrDefaultAsync(c => c.CompetitionId == competitionId);

        if (competition == null) return null;

        var roundDtos = competition.RegistrationRounds
            .OrderBy(r => r.StartDate)
            .Select(r => new RegistrationRoundReadDto
            {
                RoundId   = r.RoundId,
                RoundName = r.RoundName,
                StartDate = r.StartDate,
                EndDate   = r.EndDate,
                RegistrationCount = competition.Registrations.Count(reg => reg.RoundId == r.RoundId)
            }).ToList();

        return new CompetitionDetailViewModel
        {
            CompetitionId      = competition.CompetitionId,
            CompetitionName    = competition.CompetitionName,
            Category           = competition.Category,
            Description        = competition.Description,
            Rules              = competition.Rules,
            Prize              = competition.Prize,
            StartDate          = competition.StartDate,
            EndDate            = competition.EndDate,
            SubmissionDeadline = competition.SubmissionDeadline,
            LatestRegistrationDeadline = GetLatestRegistrationDeadline(competition.RegistrationRounds),
            Status             = competition.Status,
            IsTeamBased        = competition.IsTeamBased,
            MinParticipants    = competition.MinParticipants,
            MaxParticipants    = competition.MaxParticipants,
            MaxTeamSize        = competition.MaxTeamSize,
            TotalRegistrations = competition.Registrations.Count,
            ApprovedRegistrations = competition.Registrations.Count(r => r.Status == "Approved"),
            TotalSubmissions   = competition.Submissions.Count,
            ScoringCriteria    = competition.ScoringCriteria.Select(sc => new ScoringCriteriaDto
            {
                CriteriaId   = sc.CriteriaId,
                CriteriaName = sc.CriteriaName,
                Weight       = sc.Weight,
                MaxScore     = sc.MaxScore
            }).ToList(),
            RegistrationRounds = roundDtos
        };
    }

    // ===== CREATE =====

    public async Task<int> CreateCompetitionAsync(CreateCompetitionViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.CompetitionName))
            throw new InvalidOperationException("Tên cuộc thi không được để trống.");

        if (string.IsNullOrWhiteSpace(model.Category))
            throw new InvalidOperationException("Vui lòng chọn lĩnh vực.");

        var startDate         = ToUtc(model.StartDate);
        var endDate           = ToUtc(model.EndDate);
        var submissionDeadline = ToUtc(model.SubmissionDeadline);

        if (endDate <= startDate)
            throw new InvalidOperationException("Ngày kết thúc phải sau ngày bắt đầu.");

        if (submissionDeadline > endDate)
            throw new InvalidOperationException("Hạn nộp bài phải trước hoặc bằng ngày kết thúc.");

        if (model.MinParticipants < 0)
            throw new InvalidOperationException("Số lượng tối thiểu không được âm.");

        if (model.MaxParticipants < 1)
            throw new InvalidOperationException("Số lượng tối đa phải lớn hơn 0.");

        if (model.MinParticipants > model.MaxParticipants)
            throw new InvalidOperationException("Số lượng tối thiểu không được lớn hơn số lượng tối đa.");

        if (model.IsTeamBased && model.MaxTeamSize < 2)
            throw new InvalidOperationException("Cuộc thi theo đội phải có tối thiểu 2 thành viên/đội.");

        if (!model.IsTeamBased)
            model.MaxTeamSize = 1;

        // Validate rounds
        ValidateRounds(model.RegistrationRounds, startDate);

        // Validate scoring criteria
        if (model.ScoringCriteria == null || model.ScoringCriteria.Count == 0)
            throw new InvalidOperationException("Phải có ít nhất một tiêu chí chấm điểm.");

        foreach (var criteria in model.ScoringCriteria)
        {
            if (criteria.Weight > 1m)
                criteria.Weight = criteria.Weight > 100m ? criteria.Weight / 10000m : criteria.Weight / 100m;
        }

        var totalWeight = model.ScoringCriteria.Sum(s => s.Weight);
        if (Math.Abs(totalWeight - 1.0m) > 0.01m)
            throw new InvalidOperationException($"Tổng trọng số phải bằng 100%, hiện tại là {totalWeight * 100:F1}%.");

        // Tạo cuộc thi
        var competition = new Competitions
        {
            CompetitionName    = model.CompetitionName,
            Description        = model.Description,
            Category           = model.Category,
            Rules              = model.Rules,
            Prize              = model.Prize,
            StartDate          = startDate,
            EndDate            = endDate,
            SubmissionDeadline = submissionDeadline,
            MinParticipants    = model.MinParticipants,
            MaxParticipants    = model.MaxParticipants,
            MaxTeamSize        = model.MaxTeamSize,
            IsTeamBased        = model.IsTeamBased,
            Status             = "Draft",
            CreatedAt          = DateTime.UtcNow
        };

        _context.Competitions.Add(competition);
        await _context.SaveChangesAsync();

        // Tạo các đợt đăng ký
        foreach (var round in model.RegistrationRounds)
        {
            _context.RegistrationRounds.Add(new RegistrationRounds
            {
                CompetitionId = competition.CompetitionId,
                RoundName     = round.RoundName,
                StartDate     = ToUtc(round.StartDate),
                EndDate       = ToUtc(round.EndDate),
                CreatedAt     = DateTime.UtcNow
            });
        }

        // Tạo tiêu chí chấm điểm
        foreach (var criteria in model.ScoringCriteria)
        {
            _context.ScoringCriteria.Add(new ScoringCriteria
            {
                CompetitionId = competition.CompetitionId,
                CriteriaName  = criteria.CriteriaName,
                Description   = criteria.Description,
                MaxScore      = criteria.MaxScore,
                Weight        = criteria.Weight,
                Order         = criteria.Order,
                CreatedAt     = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();

        // Tạo nhà tài trợ
        if (model.Sponsors != null && model.Sponsors.Any())
        {
            foreach (var sponsorDto in model.Sponsors.Where(s => !string.IsNullOrWhiteSpace(s.SponsorName)))
            {
                var sponsor = new Sponsors
                {
                    SponsorName = sponsorDto.SponsorName,
                    Email       = sponsorDto.Email,
                    PhoneNumber = sponsorDto.PhoneNumber,
                    Website     = sponsorDto.Website,
                    LogoUrl     = sponsorDto.LogoUrl,
                    Description = sponsorDto.Description,
                    Status      = "Active",
                    CreatedAt   = DateTime.UtcNow
                };
                _context.Sponsors.Add(sponsor);
                await _context.SaveChangesAsync();

                _context.CompetitionSponsors.Add(new CompetitionSponsors
                {
                    CompetitionId     = competition.CompetitionId,
                    SponsorId         = sponsor.SponsorId,
                    SponsorshipLevel  = sponsorDto.SponsorshipLevel,
                    ContributionAmount = sponsorDto.ContributionAmount,
                    Currency          = sponsorDto.Currency,
                    Notes             = sponsorDto.Notes,
                    IsDisplayed       = sponsorDto.IsDisplayed,
                    DisplayOrder      = sponsorDto.DisplayOrder,
                    SponsoredAt       = DateTime.UtcNow
                });
            }
            await _context.SaveChangesAsync();
        }

        await SaveCompetitionFilesAsync(competition.CompetitionId, model);

        return competition.CompetitionId;
    }

    private async Task SaveCompetitionFilesAsync(int competitionId, CreateCompetitionViewModel model)
    {
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

                    var header = dataUrl[..commaIdx];
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
                        ImageUrl      = $"/images/competitions/{competitionId}/{fileName}",
                        IsThumbnail   = isFirst,
                        CreatedAt     = DateTime.UtcNow
                    });
                    isFirst = false;
                }
                catch { /* bỏ qua file bị lỗi */ }
            }
            await _context.SaveChangesAsync();
        }

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

                    var base64       = dataUrl[(commaIdx + 1)..];
                    var originalName = i < fileNames.Count ? fileNames[i] : $"document_{i + 1}";
                    var ext          = Path.GetExtension(originalName).TrimStart('.');
                    if (string.IsNullOrEmpty(ext)) ext = "pdf";

                    var fileName = $"{Guid.NewGuid()}.{ext}";
                    var filePath = Path.Combine(docDir, fileName);
                    await File.WriteAllBytesAsync(filePath, Convert.FromBase64String(base64));

                    _context.CompetitionDocuments.Add(new CompetitionDocuments
                    {
                        CompetitionId = competitionId,
                        FileName      = originalName,
                        FileUrl       = $"/pdf_excel/competitions/{competitionId}/{fileName}",
                        FileType      = ext.ToLower(),
                        UploadedAt    = DateTime.UtcNow
                    });
                }
                catch { /* bỏ qua file bị lỗi */ }
            }
            await _context.SaveChangesAsync();
        }
    }

    // ===== GET FOR EDIT =====

    public async Task<EditCompetitionViewModel> GetCompetitionForEditAsync(int competitionId)
    {
        var competition = await _context.Competitions
            .Include(c => c.ScoringCriteria)
            .Include(c => c.RegistrationRounds)
                .ThenInclude(r => r.Registrations)
            .Include(c => c.CompetitionImages)
            .Include(c => c.CompetitionDocuments)
            .Include(c => c.CompetitionSponsors)
                .ThenInclude(cs => cs.Sponsor)
            .FirstOrDefaultAsync(c => c.CompetitionId == competitionId);

        if (competition == null) return null;

        var now = DateTime.UtcNow;
        var totalRegistrations = await _context.Registrations
            .CountAsync(r => r.CompetitionId == competitionId);
        var submissionCount = await _context.Submissions
            .CountAsync(s => s.CompetitionId == competitionId);

        var existingRounds = competition.RegistrationRounds
            .OrderBy(r => r.StartDate)
            .Select(r => new RegistrationRoundReadDto
            {
                RoundId           = r.RoundId,
                RoundName         = r.RoundName,
                StartDate         = r.StartDate,
                EndDate           = r.EndDate,
                RegistrationCount = r.Registrations.Count
            }).ToList();

        var scoringCriteria = competition.ScoringCriteria
            .OrderBy(sc => sc.Order)
            .Select(sc => new ScoringCriteriaCreateDto
            {
                CriteriaName = sc.CriteriaName,
                Description  = sc.Description,
                MaxScore     = sc.MaxScore,
                Weight       = sc.Weight,
                Order        = sc.Order
            }).ToList();

        var images = competition.CompetitionImages
            .OrderByDescending(i => i.IsThumbnail)
            .ThenBy(i => i.CreatedAt)
            .Select(i => new ImageReadDto
            {
                ImageId     = i.ImageId,
                ImageUrl    = i.ImageUrl,
                IsThumbnail = i.IsThumbnail,
                CreatedAt   = i.CreatedAt
            }).ToList();

        var documents = competition.CompetitionDocuments
            .OrderBy(d => d.UploadedAt)
            .Select(d => new DocumentReadDto
            {
                DocumentId = d.DocumentId,
                FileName   = d.FileName,
                FileUrl    = d.FileUrl,
                FileType   = d.FileType,
                UploadedAt = d.UploadedAt
            }).ToList();

        var competitionSponsors = competition.CompetitionSponsors
            .OrderBy(cs => cs.DisplayOrder)
            .Select(cs => new CompetitionSponsorReadDto
            {
                CompetitionSponsorId = cs.CompetitionSponsorId,
                SponsorId            = cs.SponsorId,
                SponsorName          = cs.Sponsor.SponsorName,
                LogoUrl              = cs.Sponsor.LogoUrl,
                SponsorshipLevel     = cs.SponsorshipLevel,
                ContributionAmount   = cs.ContributionAmount,
                Currency             = cs.Currency,
                Notes                = cs.Notes,
                IsDisplayed          = cs.IsDisplayed,
                DisplayOrder         = cs.DisplayOrder
            }).ToList();

        return new EditCompetitionViewModel
        {
            CompetitionId      = competition.CompetitionId,
            CompetitionName    = competition.CompetitionName,
            Description        = competition.Description,
            Category           = competition.Category,
            Rules              = competition.Rules,
            Prize              = competition.Prize,
            StartDate          = competition.StartDate,
            EndDate            = competition.EndDate,
            SubmissionDeadline = competition.SubmissionDeadline,
            MinParticipants    = competition.MinParticipants,
            MaxParticipants    = competition.MaxParticipants,
            MaxTeamSize        = competition.MaxTeamSize,
            IsTeamBased        = competition.IsTeamBased,
            ScoringCriteria    = scoringCriteria,
            ExistingRounds     = existingRounds,
            RegistrationCount  = totalRegistrations,
            SubmissionCount    = submissionCount,
            HasStarted         = now >= competition.StartDate,
            HasSubmissions     = submissionCount > 0,
            Images             = images,
            Documents          = documents,
            ExistingCompetitionSponsors = competitionSponsors
        };
    }

    // ===== UPDATE =====

    public async Task<bool> UpdateCompetitionAsync(int competitionId, EditCompetitionViewModel model)
    {
        var competition = await _context.Competitions
            .Include(c => c.ScoringCriteria)
            .Include(c => c.RegistrationRounds)
                .ThenInclude(r => r.Registrations)
            .FirstOrDefaultAsync(c => c.CompetitionId == competitionId);

        if (competition == null) return false;

        var now              = DateTime.UtcNow;
        var hasRegistrations = competition.Registrations?.Any() == true
            || await _context.Registrations.AnyAsync(r => r.CompetitionId == competitionId);
        var hasSubmissions   = await _context.Submissions.AnyAsync(s => s.CompetitionId == competitionId);
        var hasStarted       = now >= competition.StartDate;

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // === Các trường luôn được phép sửa ===
            competition.Description = model.Description;
            competition.Rules       = model.Rules;
            competition.Prize       = model.Prize;
            competition.UpdatedAt   = now;

            // === Trường hợp A: Chưa có đăng ký — sửa toàn bộ ===
            if (!hasRegistrations)
            {
                var startDate          = ToUtc(model.StartDate);
                var endDate            = ToUtc(model.EndDate);
                var submissionDeadline = ToUtc(model.SubmissionDeadline);

                if (endDate <= startDate)
                    throw new InvalidOperationException("Ngày kết thúc phải sau ngày bắt đầu.");

                if (submissionDeadline > endDate)
                    throw new InvalidOperationException("Hạn nộp bài phải trước hoặc bằng ngày kết thúc.");

                if (model.MinParticipants > model.MaxParticipants)
                    throw new InvalidOperationException("Số lượng tối thiểu không được lớn hơn số lượng tối đa.");

                if (model.IsTeamBased && model.MaxTeamSize < 2)
                    throw new InvalidOperationException("Cuộc thi theo đội phải có tối thiểu 2 thành viên/đội.");

                competition.CompetitionName    = model.CompetitionName;
                competition.Category           = model.Category;
                competition.StartDate          = startDate;
                competition.EndDate            = endDate;
                competition.SubmissionDeadline = submissionDeadline;
                competition.MinParticipants    = model.MinParticipants;
                competition.MaxParticipants    = model.MaxParticipants;
                competition.MaxTeamSize        = model.IsTeamBased ? model.MaxTeamSize : 1;
                competition.IsTeamBased        = model.IsTeamBased;

                // Xóa rounds cũ và tạo lại
                _context.RegistrationRounds.RemoveRange(competition.RegistrationRounds);

                if (model.RegistrationRounds != null && model.RegistrationRounds.Count > 0)
                {
                    ValidateRounds(model.RegistrationRounds, startDate);
                    foreach (var round in model.RegistrationRounds)
                    {
                        _context.RegistrationRounds.Add(new RegistrationRounds
                        {
                            CompetitionId = competitionId,
                            RoundName     = round.RoundName,
                            StartDate     = ToUtc(round.StartDate),
                            EndDate       = ToUtc(round.EndDate),
                            CreatedAt     = now
                        });
                    }
                }

                // Xóa tiêu chí cũ và tạo lại
                if (model.ScoringCriteria != null && model.ScoringCriteria.Any())
                {
                    _context.ScoringCriteria.RemoveRange(competition.ScoringCriteria);
                    foreach (var criteria in model.ScoringCriteria)
                    {
                        _context.ScoringCriteria.Add(new ScoringCriteria
                        {
                            CompetitionId = competitionId,
                            CriteriaName  = criteria.CriteriaName,
                            Description   = criteria.Description,
                            MaxScore      = criteria.MaxScore,
                            Weight        = criteria.Weight,
                            Order         = criteria.Order,
                            CreatedAt     = now
                        });
                    }
                }
            }
            else // === Đã có đăng ký ===
            {
                // === Trường hợp E: Đã có bài nộp — khóa tiêu chí và lịch trình ===
                if (hasSubmissions)
                {
                    // Không cho sửa ScoringCriteria, StartDate, EndDate, SubmissionDeadline,
                    // MaxParticipants, MaxTeamSize, IsTeamBased, RegistrationRounds
                    // Chỉ còn Description/Rules/Prize đã cập nhật ở trên
                }
                else
                {
                    // === Trường hợp D: Đã bắt đầu — khóa lịch trình và cấu trúc ===
                    if (hasStarted)
                    {
                        // Không cho sửa StartDate, EndDate, RegistrationRounds,
                        // MaxParticipants, MaxTeamSize, IsTeamBased
                        // Cho phép sửa ScoringCriteria (chưa có bài nộp)
                        if (model.ScoringCriteria != null && model.ScoringCriteria.Any())
                        {
                            _context.ScoringCriteria.RemoveRange(competition.ScoringCriteria);
                            foreach (var criteria in model.ScoringCriteria)
                            {
                                _context.ScoringCriteria.Add(new ScoringCriteria
                                {
                                    CompetitionId = competitionId,
                                    CriteriaName  = criteria.CriteriaName,
                                    Description   = criteria.Description,
                                    MaxScore      = criteria.MaxScore,
                                    Weight        = criteria.Weight,
                                    Order         = criteria.Order,
                                    CreatedAt     = now
                                });
                            }
                        }
                    }
                    else
                    {
                        // === Trường hợp B+C: Đã có đăng ký nhưng chưa bắt đầu ===

                        // Không cho đổi IsTeamBased
                        if (model.IsTeamBased != competition.IsTeamBased)
                            throw new InvalidOperationException("Không thể thay đổi hình thức thi sau khi đã có người đăng ký.");

                        // Không cho giảm MaxParticipants dưới số đã đăng ký
                        var currentRegCount = await _context.Registrations
                            .CountAsync(r => r.CompetitionId == competitionId);
                        if (model.MaxParticipants < currentRegCount)
                            throw new InvalidOperationException(
                                $"Không thể giảm số lượng tối đa xuống {model.MaxParticipants} khi đã có {currentRegCount} người đăng ký.");

                        // Cho phép dời ngày (C)
                        var newStartDate          = ToUtc(model.StartDate);
                        var newEndDate            = ToUtc(model.EndDate);
                        var newSubmissionDeadline = ToUtc(model.SubmissionDeadline);

                        if (newEndDate <= newStartDate)
                            throw new InvalidOperationException("Ngày kết thúc phải sau ngày bắt đầu.");

                        if (newSubmissionDeadline > newEndDate)
                            throw new InvalidOperationException("Hạn nộp bài phải trước hoặc bằng ngày kết thúc.");

                        competition.StartDate          = newStartDate;
                        competition.EndDate            = newEndDate;
                        competition.SubmissionDeadline = newSubmissionDeadline;
                        competition.MinParticipants    = model.MinParticipants;
                        competition.MaxParticipants    = model.MaxParticipants;

                        // Cập nhật ScoringCriteria (B - chưa có bài nộp)
                        if (model.ScoringCriteria != null && model.ScoringCriteria.Any())
                        {
                            _context.ScoringCriteria.RemoveRange(competition.ScoringCriteria);
                            foreach (var criteria in model.ScoringCriteria)
                            {
                                _context.ScoringCriteria.Add(new ScoringCriteria
                                {
                                    CompetitionId = competitionId,
                                    CriteriaName  = criteria.CriteriaName,
                                    Description   = criteria.Description,
                                    MaxScore      = criteria.MaxScore,
                                    Weight        = criteria.Weight,
                                    Order         = criteria.Order,
                                    CreatedAt     = now
                                });
                            }
                        }

                        // Thêm round mới nếu được gửi (C - gia hạn đăng ký)
                        if (model.NewRound != null && !string.IsNullOrWhiteSpace(model.NewRound.RoundName))
                        {
                            var newRoundDto = new List<RegistrationRoundCreateDto> { model.NewRound };
                            ValidateRounds(newRoundDto, newStartDate);

                            // Kiểm tra không chồng chéo với rounds hiện có
                            var existingRounds = competition.RegistrationRounds.ToList();
                            var newStart = ToUtc(model.NewRound.StartDate);
                            var newEnd   = ToUtc(model.NewRound.EndDate);

                            foreach (var existing in existingRounds)
                            {
                                if (newStart <= existing.EndDate && newEnd >= existing.StartDate)
                                    throw new InvalidOperationException(
                                        $"Đợt mới chồng chéo với đợt \"{existing.RoundName}\" ({existing.StartDate:dd/MM/yyyy} - {existing.EndDate:dd/MM/yyyy}).");
                            }

                            _context.RegistrationRounds.Add(new RegistrationRounds
                            {
                                CompetitionId = competitionId,
                                RoundName     = model.NewRound.RoundName,
                                StartDate     = newStart,
                                EndDate       = newEnd,
                                CreatedAt     = now
                            });
                        }
                    }
                }
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

    // ===== ADD REGISTRATION ROUND =====

    public async Task<bool> AddRegistrationRoundAsync(int competitionId, RegistrationRoundCreateDto roundDto)
    {
        var competition = await _context.Competitions
            .Include(c => c.RegistrationRounds)
            .FirstOrDefaultAsync(c => c.CompetitionId == competitionId);

        if (competition == null) return false;

        var now = DateTime.UtcNow;

        if (now >= competition.StartDate)
            throw new InvalidOperationException("Không thể thêm đợt đăng ký sau khi cuộc thi đã bắt đầu.");

        var newRoundList = new List<RegistrationRoundCreateDto> { roundDto };
        ValidateRounds(newRoundList, competition.StartDate);

        var newStart = ToUtc(roundDto.StartDate);
        var newEnd   = ToUtc(roundDto.EndDate);

        foreach (var existing in competition.RegistrationRounds)
        {
            if (newStart <= existing.EndDate && newEnd >= existing.StartDate)
                throw new InvalidOperationException(
                    $"Đợt mới chồng chéo với đợt \"{existing.RoundName}\" ({existing.StartDate:dd/MM/yyyy} - {existing.EndDate:dd/MM/yyyy}).");
        }

        _context.RegistrationRounds.Add(new RegistrationRounds
        {
            CompetitionId = competitionId,
            RoundName     = roundDto.RoundName,
            StartDate     = newStart,
            EndDate       = newEnd,
            CreatedAt     = now
        });

        await _context.SaveChangesAsync();
        return true;
    }

    // ===== DELETE =====

    public async Task<bool> DeleteCompetitionAsync(int competitionId)
    {
        var competition = await _context.Competitions
            .Include(c => c.Registrations)
            .Include(c => c.ScoringCriteria)
            .Include(c => c.RegistrationRounds)
            .FirstOrDefaultAsync(c => c.CompetitionId == competitionId);

        if (competition == null) return false;

        if (competition.Registrations.Count > 0)
            throw new InvalidOperationException("Không thể xóa cuộc thi có người đăng ký.");

        _context.ScoringCriteria.RemoveRange(competition.ScoringCriteria);
        _context.RegistrationRounds.RemoveRange(competition.RegistrationRounds);
        _context.Competitions.Remove(competition);
        await _context.SaveChangesAsync();
        return true;
    }

    // ===== CHANGE STATUS =====

    public async Task<bool> ChangeCompetitionStatusAsync(int competitionId, string newStatus)
    {
        var competition = await _context.Competitions
            .FirstOrDefaultAsync(c => c.CompetitionId == competitionId);

        if (competition == null) return false;

        var validStatuses = new[] { "Draft", "Active", "Closed", "Completed" };
        if (!validStatuses.Contains(newStatus))
            throw new InvalidOperationException("Trạng thái không hợp lệ.");

        competition.Status    = newStatus;
        competition.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    // ===== DASHBOARD =====

    public async Task<OrganizerDashboardViewModel> GetOrganizerDashboardDataAsync()
    {
        var now = DateTime.UtcNow;

        var competitions = await _context.Competitions
            .Include(c => c.Registrations)
            .Include(c => c.Submissions)
            .Include(c => c.RegistrationRounds)
            .ToListAsync();

        var judges = await _context.Judges.ToListAsync();

        var activeCompetitions   = competitions.Count(c => c.Status == "Active");
        var pendingRegistrations = await _context.Registrations.CountAsync(r => r.Status == "Pending");
        var totalSubmissions     = competitions.Sum(c => c.Submissions.Count);
        var evaluatedSubmissions = await _context.Submissions.CountAsync(s => s.Status == "Evaluated");
        var activeJudges         = judges.Count;
        var urgentCompetitions   = competitions.Count(c =>
            c.Status == "Active" && (c.EndDate - now).TotalDays < 7 && (c.EndDate - now).TotalDays >= 0);

        // Progress data (4 tuần gần nhất)
        var progressData       = new List<CompetitionProgressData>();
        var activeCompetition  = competitions.FirstOrDefault(c => c.Status == "Active");
        if (activeCompetition != null)
        {
            for (int i = 3; i >= 0; i--)
            {
                var weekStart = now.AddDays(-7 * i);
                var weekEnd   = weekStart.AddDays(7);

                var regsInWeek = await _context.Registrations
                    .CountAsync(r => r.CompetitionId == activeCompetition.CompetitionId
                        && r.RegistrationDate >= weekStart && r.RegistrationDate < weekEnd);

                var subsInWeek = await _context.Submissions
                    .CountAsync(s => s.CompetitionId == activeCompetition.CompetitionId
                        && s.SubmissionDate >= weekStart && s.SubmissionDate < weekEnd);

                progressData.Add(new CompetitionProgressData
                {
                    Week          = $"Tuần {4 - i}",
                    Registrations = regsInWeek,
                    Submissions   = subsInWeek
                });
            }
        }

        // Approval ratio
        var allRegistrations = await _context.Registrations.ToListAsync();
        var approvalRatio = new ApprovalRatioData
        {
            ApprovedCount = allRegistrations.Count(r => r.Status == "Approved"),
            PendingCount  = allRegistrations.Count(r => r.Status == "Pending"),
            RejectedCount = allRegistrations.Count(r => r.Status == "Rejected")
        };

        // Deadlines — dùng rounds thay cho RegistrationDeadline
        var deadlines = new List<DeadlineItem>();
        foreach (var comp in competitions.Where(c => c.Status == "Active" || c.Status == "Draft"))
        {
            var latestDeadline = GetLatestRegistrationDeadline(comp.RegistrationRounds);

            if (latestDeadline.HasValue && latestDeadline.Value > now)
            {
                var daysUntil = (latestDeadline.Value - now).TotalDays;
                deadlines.Add(new DeadlineItem
                {
                    CompetitionId   = comp.CompetitionId,
                    CompetitionName = comp.CompetitionName,
                    Title           = "Đóng cổng Nhận hồ sơ",
                    DeadlineDate    = latestDeadline.Value,
                    Status          = daysUntil < 3 ? "urgent" : daysUntil < 7 ? "warning" : "normal",
                    Description     = $"Cuộc thi: {comp.CompetitionName}. Đã nhận {comp.Registrations.Count} hồ sơ."
                });
            }

            if (comp.SubmissionDeadline > now)
            {
                var daysUntil = (comp.SubmissionDeadline - now).TotalDays;
                deadlines.Add(new DeadlineItem
                {
                    CompetitionId   = comp.CompetitionId,
                    CompetitionName = comp.CompetitionName,
                    Title           = "Đóng cổng Nhận bài dự thi",
                    DeadlineDate    = comp.SubmissionDeadline,
                    Status          = daysUntil < 3 ? "urgent" : daysUntil < 7 ? "warning" : "normal",
                    Description     = $"Cuộc thi: {comp.CompetitionName}. Đã nhận {comp.Submissions.Count} bài thi."
                });
            }

            if (comp.EndDate > now && comp.Submissions.Count > 0)
            {
                var evaluatedCount  = comp.Submissions.Count(s => s.Status == "Evaluated");
                var progressPercent = (decimal)evaluatedCount / comp.Submissions.Count * 100;
                var daysUntil       = (comp.EndDate - now).TotalDays;

                deadlines.Add(new DeadlineItem
                {
                    CompetitionId      = comp.CompetitionId,
                    CompetitionName    = comp.CompetitionName,
                    Title              = "Hạn nộp điểm của Giám khảo",
                    DeadlineDate       = comp.EndDate,
                    Status             = daysUntil < 3 ? "urgent" : daysUntil < 7 ? "warning" : "normal",
                    ProgressPercentage = progressPercent,
                    Description        = $"Cuộc thi: {comp.CompetitionName}."
                });
            }
        }

        deadlines = deadlines.OrderBy(d => d.DeadlineDate).ToList();

        // Recent activities
        var recentActivities = new List<ActivityLog>();

        var recentScores = await _context.Scores
            .Include(s => s.Submission)
            .OrderByDescending(s => s.ScoredDate)
            .Take(5)
            .ToListAsync();

        foreach (var score in recentScores)
        {
            recentActivities.Add(new ActivityLog
            {
                Type        = "score",
                Title       = "Giám khảo chấm bài hoàn tất",
                Description = $"Bài thi #{score.SubmissionId} đã được chấm.",
                CreatedAt   = score.ScoredDate,
                UserName    = "Giám khảo"
            });
        }

        var recentRegistrations = await _context.Registrations
            .Include(r => r.User)
            .Where(r => r.Status == "Pending")
            .OrderByDescending(r => r.RegistrationDate)
            .Take(3)
            .ToListAsync();

        foreach (var reg in recentRegistrations)
        {
            recentActivities.Add(new ActivityLog
            {
                Type        = "registration",
                Title       = "Hồ sơ đăng ký mới chờ duyệt",
                Description = $"Sinh viên {reg.User?.FullName ?? "Unknown"} đã nộp hồ sơ.",
                CreatedAt   = reg.RegistrationDate,
                UserName    = reg.User?.FullName
            });
        }

        var recentSubmissions = await _context.Submissions
            .OrderByDescending(s => s.SubmissionDate)
            .Take(2)
            .ToListAsync();

        foreach (var sub in recentSubmissions)
        {
            recentActivities.Add(new ActivityLog
            {
                Type        = "submission",
                Title       = "Bài dự thi mới được nộp",
                Description = $"Bài thi #{sub.SubmissionId} đã được nộp.",
                CreatedAt   = sub.SubmissionDate
            });
        }

        recentActivities = recentActivities.OrderByDescending(a => a.CreatedAt).ToList();

        return new OrganizerDashboardViewModel
        {
            ActiveCompetitions   = activeCompetitions,
            PendingRegistrations = pendingRegistrations,
            TotalSubmissions     = totalSubmissions,
            EvaluatedSubmissions = evaluatedSubmissions,
            ActiveJudges         = activeJudges,
            UrgentCompetitions   = urgentCompetitions,
            ProgressData         = progressData,
            ApprovalRatio        = approvalRatio,
            UpcomingDeadlines    = deadlines,
            RecentActivities     = recentActivities
        };
    }

    // ===== IMAGES =====

    public async Task<bool> UploadImagesAsync(int competitionId, List<string> base64DataList)
    {
        if (!await _context.Competitions.AnyAsync(c => c.CompetitionId == competitionId))
            return false;

        var imageDir = Path.Combine(_env.WebRootPath, "images", "competitions", competitionId.ToString());
        Directory.CreateDirectory(imageDir);

        var hasThumbnail = await _context.CompetitionImages
            .AnyAsync(i => i.CompetitionId == competitionId && i.IsThumbnail);

        bool isFirst = !hasThumbnail;

        foreach (var dataUrl in base64DataList.Where(d => !string.IsNullOrWhiteSpace(d)))
        {
            try
            {
                var commaIdx = dataUrl.IndexOf(',');
                if (commaIdx < 0) continue;

                var header = dataUrl[..commaIdx];
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
                    ImageUrl      = $"/images/competitions/{competitionId}/{fileName}",
                    IsThumbnail   = isFirst,
                    CreatedAt     = DateTime.UtcNow
                });
                isFirst = false;
            }
            catch { /* bỏ qua file lỗi */ }
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteImageAsync(int imageId, int competitionId)
    {
        var image = await _context.CompetitionImages
            .FirstOrDefaultAsync(i => i.ImageId == imageId && i.CompetitionId == competitionId);
        if (image == null) return false;

        // Xóa file vật lý
        var physicalPath = Path.Combine(_env.WebRootPath, image.ImageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(physicalPath)) File.Delete(physicalPath);

        var wasThumbnail = image.IsThumbnail;
        _context.CompetitionImages.Remove(image);
        await _context.SaveChangesAsync();

        // Nếu xóa thumbnail thì gán thumbnail cho ảnh đầu tiên còn lại
        if (wasThumbnail)
        {
            var next = await _context.CompetitionImages
                .Where(i => i.CompetitionId == competitionId)
                .OrderBy(i => i.CreatedAt)
                .FirstOrDefaultAsync();
            if (next != null)
            {
                next.IsThumbnail = true;
                await _context.SaveChangesAsync();
            }
        }

        return true;
    }

    public async Task<bool> SetThumbnailAsync(int imageId, int competitionId)
    {
        var images = await _context.CompetitionImages
            .Where(i => i.CompetitionId == competitionId)
            .ToListAsync();

        var target = images.FirstOrDefault(i => i.ImageId == imageId);
        if (target == null) return false;

        foreach (var img in images) img.IsThumbnail = img.ImageId == imageId;
        await _context.SaveChangesAsync();
        return true;
    }

    // ===== DOCUMENTS =====

    private static readonly HashSet<string> AllowedDocExtensions = new(StringComparer.OrdinalIgnoreCase)
        { "pdf", "doc", "docx", "xls", "xlsx", "ppt", "pptx" };

    public async Task<bool> UploadDocumentsAsync(int competitionId, List<string> base64DataList, List<string> fileNames)
    {
        if (!await _context.Competitions.AnyAsync(c => c.CompetitionId == competitionId))
            return false;

        var docDir = Path.Combine(_env.WebRootPath, "pdf_excel", "competitions", competitionId.ToString());
        Directory.CreateDirectory(docDir);

        for (int i = 0; i < base64DataList.Count; i++)
        {
            var dataUrl = base64DataList[i];
            if (string.IsNullOrWhiteSpace(dataUrl)) continue;

            try
            {
                var commaIdx = dataUrl.IndexOf(',');
                if (commaIdx < 0) continue;

                var base64       = dataUrl[(commaIdx + 1)..];
                var originalName = i < fileNames.Count ? fileNames[i] : $"document_{i + 1}";
                var ext          = Path.GetExtension(originalName).TrimStart('.').ToLower();
                if (!AllowedDocExtensions.Contains(ext)) continue;

                var fileName = $"{Guid.NewGuid()}.{ext}";
                var filePath = Path.Combine(docDir, fileName);
                await File.WriteAllBytesAsync(filePath, Convert.FromBase64String(base64));

                _context.CompetitionDocuments.Add(new CompetitionDocuments
                {
                    CompetitionId = competitionId,
                    FileName      = originalName,
                    FileUrl       = $"/pdf_excel/competitions/{competitionId}/{fileName}",
                    FileType      = ext,
                    UploadedAt    = DateTime.UtcNow
                });
            }
            catch { /* bỏ qua file lỗi */ }
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteDocumentAsync(int documentId, int competitionId)
    {
        var doc = await _context.CompetitionDocuments
            .FirstOrDefaultAsync(d => d.DocumentId == documentId && d.CompetitionId == competitionId);
        if (doc == null) return false;

        var physicalPath = Path.Combine(_env.WebRootPath, doc.FileUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(physicalPath)) File.Delete(physicalPath);

        _context.CompetitionDocuments.Remove(doc);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<(byte[] data, string fileName, string contentType)?> GetDocumentFileAsync(int documentId, int competitionId)
    {
        var doc = await _context.CompetitionDocuments
            .FirstOrDefaultAsync(d => d.DocumentId == documentId && d.CompetitionId == competitionId);
        if (doc == null) return null;

        var physicalPath = Path.Combine(_env.WebRootPath, doc.FileUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(physicalPath)) return null;

        var bytes = await File.ReadAllBytesAsync(physicalPath);
        var contentType = doc.FileType.ToLower() switch
        {
            "pdf"  => "application/pdf",
            "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            _      => "application/octet-stream"
        };

        return (bytes, doc.FileName, contentType);
    }

    // ===== SPONSORS =====

    public async Task<List<SponsorSearchDto>> GetAllSponsorsForSearchAsync()
    {
        return await _context.Sponsors
            .Where(s => s.Status == "Active")
            .OrderBy(s => s.SponsorName)
            .Select(s => new SponsorSearchDto
            {
                SponsorId   = s.SponsorId,
                SponsorName = s.SponsorName,
                Email       = s.Email,
                LogoUrl     = s.LogoUrl
            })
            .ToListAsync();
    }

    public async Task<bool> AddSponsorToCompetitionAsync(int competitionId, AddSponsorToCompetitionDto dto)
    {
        if (!await _context.Competitions.AnyAsync(c => c.CompetitionId == competitionId))
            return false;

        if (!await _context.Sponsors.AnyAsync(s => s.SponsorId == dto.SponsorId))
            throw new InvalidOperationException("Nhà tài trợ không tồn tại.");

        var alreadyLinked = await _context.CompetitionSponsors
            .AnyAsync(cs => cs.CompetitionId == competitionId && cs.SponsorId == dto.SponsorId);
        if (alreadyLinked)
            throw new InvalidOperationException("Nhà tài trợ này đã được liên kết với cuộc thi.");

        _context.CompetitionSponsors.Add(new CompetitionSponsors
        {
            CompetitionId      = competitionId,
            SponsorId          = dto.SponsorId,
            SponsorshipLevel   = dto.SponsorshipLevel,
            ContributionAmount = dto.ContributionAmount,
            Currency           = dto.Currency,
            Notes              = dto.Notes,
            IsDisplayed        = dto.IsDisplayed,
            DisplayOrder       = dto.DisplayOrder,
            SponsoredAt        = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CreateAndLinkSponsorAsync(int competitionId, SponsorCreateDto dto)
    {
        if (!await _context.Competitions.AnyAsync(c => c.CompetitionId == competitionId))
            return false;

        if (string.IsNullOrWhiteSpace(dto.SponsorName))
            throw new InvalidOperationException("Tên nhà tài trợ không được để trống.");

        var sponsor = new Sponsors
        {
            SponsorName = dto.SponsorName,
            Email       = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            Website     = dto.Website,
            LogoUrl     = dto.LogoUrl,
            Description = dto.Description,
            Status      = "Active",
            CreatedAt   = DateTime.UtcNow
        };
        _context.Sponsors.Add(sponsor);
        await _context.SaveChangesAsync();

        _context.CompetitionSponsors.Add(new CompetitionSponsors
        {
            CompetitionId      = competitionId,
            SponsorId          = sponsor.SponsorId,
            SponsorshipLevel   = dto.SponsorshipLevel,
            ContributionAmount = dto.ContributionAmount,
            Currency           = dto.Currency,
            Notes              = dto.Notes,
            IsDisplayed        = dto.IsDisplayed,
            DisplayOrder       = dto.DisplayOrder,
            SponsoredAt        = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveSponsorFromCompetitionAsync(int competitionSponsorId, int competitionId)
    {
        var link = await _context.CompetitionSponsors
            .FirstOrDefaultAsync(cs => cs.CompetitionSponsorId == competitionSponsorId
                                    && cs.CompetitionId == competitionId);
        if (link == null) return false;

        _context.CompetitionSponsors.Remove(link);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateSponsorLinkAsync(int competitionSponsorId, AddSponsorToCompetitionDto dto)
    {
        var link = await _context.CompetitionSponsors
            .FirstOrDefaultAsync(cs => cs.CompetitionSponsorId == competitionSponsorId);
        if (link == null) return false;

        link.SponsorshipLevel   = dto.SponsorshipLevel;
        link.ContributionAmount = dto.ContributionAmount;
        link.Currency           = dto.Currency;
        link.Notes              = dto.Notes;
        link.IsDisplayed        = dto.IsDisplayed;
        link.DisplayOrder       = dto.DisplayOrder;
        link.UpdatedAt          = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }
}
