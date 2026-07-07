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

    private void ValidateCompetitionRounds(List<CompetitionRoundCreateDto>? rounds, DateTime submissionDeadline, DateTime competitionEndDate)
    {
        if (rounds == null || rounds.Count == 0) return; // Vòng thi không bắt buộc

        var sorted = rounds
            .Select(r => (start: ToUtc(r.StartDate), end: ToUtc(r.EndDate), name: r.RoundName))
            .OrderBy(r => r.start)
            .ToList();

        foreach (var r in sorted)
        {
            if (r.end <= r.start)
                throw new InvalidOperationException($"Vòng \"{r.name}\": Ngày kết thúc phải sau ngày bắt đầu.");
        }

        // Vòng đầu phải bắt đầu sau hạn nộp bài
        if (sorted.First().start < submissionDeadline)
            throw new InvalidOperationException(
                $"Vòng \"{sorted.First().name}\": Phải bắt đầu sau hạn nộp bài ({submissionDeadline:dd/MM/yyyy}).");

        // Các vòng phải nối tiếp nhau (vòng sau bắt đầu sau khi vòng trước kết thúc)
        for (int i = 1; i < sorted.Count; i++)
        {
            if (sorted[i].start < sorted[i - 1].end)
                throw new InvalidOperationException(
                    $"Vòng \"{sorted[i].name}\" phải bắt đầu sau khi vòng \"{sorted[i - 1].name}\" kết thúc ({sorted[i - 1].end:dd/MM/yyyy}).");
        }

        // Vòng cuối phải kết thúc trước ngày kết thúc cuộc thi
        if (sorted.Last().end > competitionEndDate)
            throw new InvalidOperationException(
                $"Vòng \"{sorted.Last().name}\": Phải kết thúc trước ngày kết thúc cuộc thi ({competitionEndDate:dd/MM/yyyy}).");
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
            .Include(c => c.RegistrationRounds)
            .Include(c => c.CompetitionRounds)
                .ThenInclude(r => r.ScoringCriteria)
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

    public async Task<bool> IsCompetitionOwnerAsync(int competitionId, int organizerId)
    {
        return await _context.Competitions
            .AnyAsync(c => c.CompetitionId == competitionId && c.CreatedByUserId == organizerId);
    }

    public async Task<OrganizerContestsViewModel> GetOrganizerContestsAsync(
        string? searchQuery, string? statusFilter, string? categoryFilter, int pageNumber, int organizerId, bool isAdmin = false)
    {
        var query = _context.Competitions
            .Where(c => isAdmin || c.CreatedByUserId == organizerId)
            .AsQueryable();

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
            .Include(c => c.Registrations)
            .Include(c => c.Submissions)
            .Include(c => c.RegistrationRounds)
                .ThenInclude(r => r.Registrations)
            .Include(c => c.CompetitionRounds)
                .ThenInclude(r => r.ScoringCriteria)
            .Include(c => c.CompetitionImages)
            .Include(c => c.CompetitionDocuments)
            .Include(c => c.CompetitionSponsors)
                .ThenInclude(cs => cs.Sponsor)
            .FirstOrDefaultAsync(c => c.CompetitionId == competitionId);

        if (competition == null) return null;

        var roundDtos = competition.RegistrationRounds
            .OrderBy(r => r.StartDate)
            .Select(r => new RegistrationRoundReadDto
            {
                RoundId           = r.RoundId,
                RoundName         = r.RoundName,
                StartDate         = r.StartDate,
                EndDate           = r.EndDate,
                RegistrationCount = r.Registrations.Count(reg => reg.Status != "Withdrawn" && reg.Status != "Rejected")
            }).ToList();

        var images = competition.CompetitionImages
            .OrderByDescending(i => i.IsThumbnail).ThenBy(i => i.CreatedAt)
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

        var sponsors = competition.CompetitionSponsors
            .Where(cs => cs.IsDisplayed)
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

        var bannerUrl = competition.CompetitionImages
            .FirstOrDefault(i => i.IsThumbnail)?.ImageUrl
            ?? competition.CompetitionImages.FirstOrDefault()?.ImageUrl;

        var evaluatedCount = competition.Submissions.Count(s => s.Status == "Evaluated");

        var compRoundDtos = competition.CompetitionRounds
            .OrderBy(r => r.RoundOrder)
            .Select(r => new CompetitionRoundDetailDto
            {
                RoundId              = r.RoundId,
                RoundName            = r.RoundName,
                RoundOrder           = r.RoundOrder,
                StartDate            = r.StartDate,
                EndDate              = r.EndDate,
                SubmissionDeadline   = r.SubmissionDeadline,
                MaxAdvancing         = r.MaxAdvancing,
                Status               = r.Status,
                RequiresFile         = r.RequiresFile,
                RequiresVideo        = r.RequiresVideo,
                RequiresOtherLink    = r.RequiresOtherLink,
                IsOnline             = r.IsOnline,
                MeetingLink          = r.MeetingLink,
                MeetingTime          = r.MeetingTime,
                Location             = r.Location,
                ScoringCriteria      = r.ScoringCriteria
                    .OrderBy(sc => sc.Order)
                    .Select(sc => new ScoringCriteriaDto
                    {
                        CriteriaId   = sc.CriteriaId,
                        CriteriaName = sc.CriteriaName,
                        Weight       = sc.Weight,
                        MaxScore     = sc.MaxScore
                    }).ToList()
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
            StatusDisplay      = GetStatusDisplay(competition),
            IsTeamBased        = competition.IsTeamBased,
            MinParticipants    = competition.MinParticipants,
            MaxParticipants    = competition.MaxParticipants,
            MaxTeamSize        = competition.MaxTeamSize,
            TotalRegistrations    = competition.Registrations.Count,
            ApprovedRegistrations = competition.Registrations.Count(r => r.Status == "Approved"),
            TotalSubmissions      = competition.Submissions.Count,
            EvaluatedSubmissions  = evaluatedCount,
            BannerImageUrl        = bannerUrl,
            CompetitionRounds  = compRoundDtos,
            RegistrationRounds = roundDtos,
            Images             = images,
            Documents          = documents,
            Sponsors           = sponsors
        };
    }

    // ===== CREATE =====

    public async Task<int> CreateCompetitionAsync(CreateCompetitionViewModel model, int organizerId)
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

        if (model.MaxParticipants < 1)
            throw new InvalidOperationException("Số lượng tối đa phải lớn hơn 0.");

        if (!model.IsTeamBased)
            model.MaxTeamSize = 1;

        // Validate đợt đăng ký
        ValidateRounds(model.RegistrationRounds, startDate);

        // Validate vòng thi (thời gian phải theo thứ tự: hạn nộp bài → vòng 1 → vòng 2 → ... → kết thúc)
        ValidateCompetitionRounds(model.CompetitionRounds, submissionDeadline, endDate);

        // Validate tiêu chí từng vòng thi
        if (model.CompetitionRounds != null)
        {
            foreach (var round in model.CompetitionRounds)
            {
                if (round.ScoringCriteria == null || round.ScoringCriteria.Count == 0) continue;
                foreach (var c in round.ScoringCriteria)
                    if (c.Weight > 1m)
                        c.Weight = c.Weight > 100m ? c.Weight / 10000m : c.Weight / 100m;
                var totalWeight = round.ScoringCriteria.Sum(s => s.Weight);
                if (Math.Abs(totalWeight - 1.0m) > 0.01m)
                    throw new InvalidOperationException($"Tổng trọng số của vòng \"{round.RoundName}\" phải bằng 100%, hiện tại là {totalWeight * 100:F1}%.");
            }
        }

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
            MinParticipants    = 0,
            MaxParticipants    = model.MaxParticipants,
            MaxTeamSize        = model.MaxTeamSize,
            IsTeamBased        = model.IsTeamBased,
            Status             = "Draft",
            CreatedAt          = DateTime.UtcNow,
            CreatedByUserId    = organizerId
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

        // Tạo các vòng thi và tiêu chí chấm điểm theo từng vòng
        if (model.CompetitionRounds != null && model.CompetitionRounds.Any())
        {
            int order = 1;
            foreach (var roundDto in model.CompetitionRounds.OrderBy(r => r.RoundOrder))
            {
                var round = new CompetitionRounds
                {
                    CompetitionId     = competition.CompetitionId,
                    RoundName         = roundDto.RoundName,
                    RoundOrder        = order++,
                    Description       = roundDto.Description,
                    StartDate         = ToUtc(roundDto.StartDate),
                    EndDate           = ToUtc(roundDto.EndDate),
                    SubmissionDeadline = roundDto.SubmissionDeadline.HasValue ? ToUtc(roundDto.SubmissionDeadline.Value) : null,
                    MaxAdvancing      = roundDto.MaxAdvancing,
                    RequiresFile      = roundDto.RequiresFile,
                    RequiresVideo     = roundDto.RequiresVideo,
                    RequiresOtherLink = roundDto.RequiresOtherLink,
                    IsOnline          = roundDto.IsOnline,
                    MeetingLink       = roundDto.MeetingLink,
                    MeetingTime       = roundDto.MeetingTime.HasValue ? ToUtc(roundDto.MeetingTime.Value) : null,
                    Location          = roundDto.Location,
                    Status            = "Upcoming",
                    CreatedAt         = DateTime.UtcNow
                };
                _context.CompetitionRounds.Add(round);
                await _context.SaveChangesAsync();

                if (roundDto.ScoringCriteria != null)
                {
                    foreach (var c in roundDto.ScoringCriteria)
                        _context.ScoringCriteria.Add(new ScoringCriteria
                        {
                            RoundId      = round.RoundId,
                            CriteriaName = c.CriteriaName,
                            Description  = c.Description,
                            MaxScore     = c.MaxScore,
                            Weight       = c.Weight,
                            Order        = c.Order,
                            CreatedAt    = DateTime.UtcNow
                        });
                }
            }
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
                    if (!AllowedDocExtensions.Contains(ext)) continue;

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

    public async Task<EditCompetitionViewModel?> GetCompetitionForEditAsync(int competitionId, int organizerId, bool isAdmin = false)
    {
        var competition = await _context.Competitions
            .Include(c => c.RegistrationRounds)
                .ThenInclude(r => r.Registrations)
            .Include(c => c.CompetitionRounds)
                .ThenInclude(r => r.ScoringCriteria)
            .Include(c => c.CompetitionImages)
            .Include(c => c.CompetitionDocuments)
            .Include(c => c.CompetitionSponsors)
                .ThenInclude(cs => cs.Sponsor)
            .FirstOrDefaultAsync(c => c.CompetitionId == competitionId && (isAdmin || c.CreatedByUserId == organizerId));

        if (competition == null) return null;

        var now = DateTime.UtcNow;
        var totalRegistrations = await _context.Registrations
            .CountAsync(r => r.CompetitionId == competitionId
                          && r.Status != "Withdrawn"
                          && r.Status != "Rejected");
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
                RegistrationCount = r.Registrations.Count(reg => reg.Status != "Withdrawn" && reg.Status != "Rejected")
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
            MaxParticipants    = competition.MaxParticipants,
            MaxTeamSize        = competition.MaxTeamSize,
            IsTeamBased        = competition.IsTeamBased,
            ExistingRounds     = existingRounds,
            ExistingCompetitionRounds = competition.CompetitionRounds
                .OrderBy(r => r.RoundOrder)
                .Select(r => new CompetitionRoundReadDto
                {
                    RoundId           = r.RoundId,
                    RoundName         = r.RoundName,
                    RoundOrder        = r.RoundOrder,
                    Description       = r.Description,
                    StartDate         = r.StartDate,
                    EndDate           = r.EndDate,
                    SubmissionDeadline = r.SubmissionDeadline,
                    MaxAdvancing      = r.MaxAdvancing,
                    Status            = r.Status,
                    RequiresFile      = r.RequiresFile,
                    RequiresVideo     = r.RequiresVideo,
                    RequiresOtherLink = r.RequiresOtherLink,
                    IsOnline          = r.IsOnline,
                    MeetingLink       = r.MeetingLink,
                    MeetingTime       = r.MeetingTime,
                    Location          = r.Location,
                    ScoringCriteria   = r.ScoringCriteria.OrderBy(sc => sc.Order).Select(sc => new ScoringCriteriaCreateDto
                    {
                        CriteriaName = sc.CriteriaName,
                        Description  = sc.Description,
                        MaxScore     = sc.MaxScore,
                        Weight       = sc.Weight,
                        Order        = sc.Order
                    }).ToList()
                }).ToList(),
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

    public async Task<bool> UpdateCompetitionAsync(int competitionId, EditCompetitionViewModel model, int organizerId, bool isAdmin = false)
    {
        var competition = await _context.Competitions
            .Include(c => c.RegistrationRounds)
                .ThenInclude(r => r.Registrations)
            .FirstOrDefaultAsync(c => c.CompetitionId == competitionId && (isAdmin || c.CreatedByUserId == organizerId));

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

            // ────────────────────────────────────────────────────────────────────
            // CASE 1: Chưa có đăng ký — sửa toàn bộ (trừ rounds, quản lý qua AJAX)
            // ────────────────────────────────────────────────────────────────────
            if (!hasRegistrations)
            {
                var startDate          = ToUtc(model.StartDate);
                var endDate            = ToUtc(model.EndDate);
                var submissionDeadline = ToUtc(model.SubmissionDeadline);

                if (endDate <= startDate)
                    throw new InvalidOperationException("Ngày kết thúc phải sau ngày bắt đầu.");
                if (submissionDeadline > endDate)
                    throw new InvalidOperationException("Hạn nộp bài phải trước hoặc bằng ngày kết thúc.");

                // Khi dời StartDate, kiểm tra các rounds hiện có vẫn kết thúc trước StartDate mới
                foreach (var r in competition.RegistrationRounds)
                {
                    if (r.EndDate > startDate)
                        throw new InvalidOperationException(
                            $"Đợt \"{r.RoundName}\" kết thúc {r.EndDate:dd/MM/yyyy HH:mm} — phải trước ngày bắt đầu cuộc thi ({startDate:dd/MM/yyyy HH:mm}). Vui lòng xóa hoặc điều chỉnh đợt đó trước.");
                }

                competition.CompetitionName    = model.CompetitionName;
                competition.Category           = model.Category;
                competition.StartDate          = startDate;
                competition.EndDate            = endDate;
                competition.SubmissionDeadline = submissionDeadline;
                competition.MaxParticipants    = model.MaxParticipants;
                competition.MaxTeamSize        = model.IsTeamBased ? model.MaxTeamSize : 1;
                competition.IsTeamBased        = model.IsTeamBased;

                // Rounds và tiêu chí chấm điểm được quản lý qua AJAX
            }
            // ────────────────────────────────────────────────────────────────────
            // Đã có đăng ký
            // ────────────────────────────────────────────────────────────────────
            else if (hasSubmissions)
            {
                // CASE 4: Đã có bài nộp — chỉ Description/Rules/Prize (đã cập nhật ở trên)
                // Mọi trường khác đều bị khóa, backend không cập nhật
            }
            else if (hasStarted)
            {
                // CASE 3: Đã bắt đầu, chưa có bài nộp — tiêu chí quản lý qua AJAX theo từng vòng
            }
            else
            {
                // CASE 2: Có đăng ký, chưa bắt đầu
                if (model.IsTeamBased != competition.IsTeamBased)
                    throw new InvalidOperationException("Không thể thay đổi hình thức thi sau khi đã có người đăng ký.");

                if (competition.IsTeamBased && model.MaxTeamSize < competition.MaxTeamSize)
                    throw new InvalidOperationException(
                        $"Không thể giảm số thành viên tối đa từ {competition.MaxTeamSize} xuống {model.MaxTeamSize} khi đã có người đăng ký.");

                var currentRegCount = await _context.Registrations
                    .CountAsync(r => r.CompetitionId == competitionId);
                if (model.MaxParticipants < currentRegCount)
                    throw new InvalidOperationException(
                        $"Không thể giảm số lượng tối đa xuống {model.MaxParticipants} khi đã có {currentRegCount} người đăng ký.");

                var newStartDate          = ToUtc(model.StartDate);
                var newEndDate            = ToUtc(model.EndDate);
                var newSubmissionDeadline = ToUtc(model.SubmissionDeadline);

                if (newEndDate <= newStartDate)
                    throw new InvalidOperationException("Ngày kết thúc phải sau ngày bắt đầu.");
                if (newSubmissionDeadline > newEndDate)
                    throw new InvalidOperationException("Hạn nộp bài phải trước hoặc bằng ngày kết thúc.");

                // Khi dời StartDate, kiểm tra rounds hiện có vẫn kết thúc trước StartDate mới
                foreach (var r in competition.RegistrationRounds)
                {
                    if (r.EndDate > newStartDate)
                        throw new InvalidOperationException(
                            $"Đợt \"{r.RoundName}\" kết thúc {r.EndDate:dd/MM/yyyy HH:mm} — phải trước ngày bắt đầu mới ({newStartDate:dd/MM/yyyy HH:mm}).");
                }

                competition.StartDate          = newStartDate;
                competition.EndDate            = newEndDate;
                competition.SubmissionDeadline = newSubmissionDeadline;
                competition.MaxParticipants    = model.MaxParticipants;

                // Rounds và tiêu chí chấm điểm được quản lý qua AJAX theo từng vòng
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

    // ===== UPDATE SCHEDULE DATES (AJAX) =====

    public async Task<(bool ok, string message)> UpdateScheduleDatesAsync(int competitionId, UpdateScheduleDatesDto dto, int organizerId, bool isAdmin = false)
    {
        var competition = await _context.Competitions
            .Include(c => c.RegistrationRounds)
            .FirstOrDefaultAsync(c => c.CompetitionId == competitionId && (isAdmin || c.CreatedByUserId == organizerId));

        if (competition == null) return (false, "Cuộc thi không tìm thấy.");

        var now        = DateTime.UtcNow;
        var hasStarted = now >= competition.StartDate;

        if (hasStarted)
            return (false, "Cuộc thi đã bắt đầu — không thể thay đổi ngày.");

        var startDate          = ToUtc(dto.StartDate);
        var endDate            = ToUtc(dto.EndDate);
        var submissionDeadline = ToUtc(dto.SubmissionDeadline);

        if (endDate <= startDate)
            return (false, "Ngày kết thúc phải sau ngày bắt đầu.");
        if (submissionDeadline > endDate)
            return (false, "Hạn nộp bài phải trước hoặc bằng ngày kết thúc.");

        foreach (var r in competition.RegistrationRounds)
        {
            if (r.EndDate > startDate)
                return (false, $"Đợt \"{r.RoundName}\" kết thúc {r.EndDate:dd/MM/yyyy HH:mm} — phải trước ngày bắt đầu mới ({startDate:dd/MM/yyyy HH:mm}).");
        }

        var hasRegistrations = await _context.Registrations.AnyAsync(r => r.CompetitionId == competitionId);
        if (hasRegistrations)
        {
            var currentRegCount = await _context.Registrations.CountAsync(r => r.CompetitionId == competitionId);
            if (dto.MaxParticipants < currentRegCount)
                return (false, $"Không thể giảm số lượng tối đa xuống {dto.MaxParticipants} khi đã có {currentRegCount} người đăng ký.");
        }

        competition.StartDate          = startDate;
        competition.EndDate            = endDate;
        competition.SubmissionDeadline = submissionDeadline;
        competition.MaxParticipants    = dto.MaxParticipants;
        if (dto.MaxTeamSize.HasValue && competition.IsTeamBased)
            competition.MaxTeamSize = dto.MaxTeamSize.Value;
        competition.UpdatedAt = now;

        await _context.SaveChangesAsync();
        return (true, "Đã lưu lịch trình thành công!");
    }

    // ===== ADD REGISTRATION ROUND =====

    public async Task<bool> AddRegistrationRoundAsync(int competitionId, RegistrationRoundCreateDto roundDto, DateTime? pendingStartDate = null)
    {
        var competition = await _context.Competitions
            .Include(c => c.RegistrationRounds)
            .FirstOrDefaultAsync(c => c.CompetitionId == competitionId);

        if (competition == null) return false;

        var now = DateTime.UtcNow;

        if (now >= competition.StartDate)
            throw new InvalidOperationException("Không thể thêm đợt đăng ký sau khi cuộc thi đã bắt đầu.");

        // Dùng ngày bắt đầu đang chờ lưu (nếu có) để tránh xung đột khi user chưa save form
        var effectiveStartDate = pendingStartDate.HasValue ? pendingStartDate.Value : competition.StartDate;

        var newRoundList = new List<RegistrationRoundCreateDto> { roundDto };
        ValidateRounds(newRoundList, effectiveStartDate);

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

    public async Task<bool> UpdateRegistrationRoundAsync(int roundId, int competitionId, RegistrationRoundUpdateDto dto)
    {
        var round = await _context.RegistrationRounds
            .Include(r => r.Registrations)
            .FirstOrDefaultAsync(r => r.RoundId == roundId && r.CompetitionId == competitionId);

        if (round == null) return false;

        var competition = await _context.Competitions
            .Include(c => c.RegistrationRounds)
            .FirstOrDefaultAsync(c => c.CompetitionId == competitionId);

        if (competition == null) return false;

        if (DateTime.UtcNow >= competition.StartDate)
            throw new InvalidOperationException("Không thể chỉnh sửa đợt đăng ký sau khi cuộc thi đã bắt đầu.");

        var newStart = ToUtc(dto.StartDate);
        var newEnd   = ToUtc(dto.EndDate);

        if (newEnd <= newStart)
            throw new InvalidOperationException("Ngày kết thúc phải sau ngày bắt đầu.");

        if (newEnd > competition.StartDate)
            throw new InvalidOperationException(
                $"Hạn đăng ký phải trước ngày bắt đầu cuộc thi ({competition.StartDate:dd/MM/yyyy HH:mm}).");

        foreach (var other in competition.RegistrationRounds.Where(r => r.RoundId != roundId))
        {
            if (newStart <= other.EndDate && newEnd >= other.StartDate)
                throw new InvalidOperationException(
                    $"Đợt đăng ký này chồng chéo với đợt \"{other.RoundName}\" ({other.StartDate:dd/MM/yyyy} - {other.EndDate:dd/MM/yyyy}).");
        }

        round.RoundName  = dto.RoundName;
        round.StartDate  = newStart;
        round.EndDate    = newEnd;

        await _context.SaveChangesAsync();
        return true;
    }

    // ===== DELETE =====

    public async Task<bool> DeleteCompetitionAsync(int competitionId, int organizerId, bool isAdmin = false)
    {
        var competition = await _context.Competitions
            .Include(c => c.Registrations)
            .Include(c => c.Teams)
            .Include(c => c.Submissions)
            .Include(c => c.RegistrationRounds)
            .FirstOrDefaultAsync(c => c.CompetitionId == competitionId && (isAdmin || c.CreatedByUserId == organizerId));

        if (competition == null) return false;

        if (competition.Registrations.Count > 0)
            throw new InvalidOperationException("Không thể xóa cuộc thi đã có người đăng ký.");

        if (competition.Submissions.Count > 0)
            throw new InvalidOperationException("Không thể xóa cuộc thi đã có bài dự thi.");

        if (competition.Teams.Count > 0)
            throw new InvalidOperationException("Không thể xóa cuộc thi đã có đội nhóm tham gia.");

        _context.RegistrationRounds.RemoveRange(competition.RegistrationRounds);
        _context.Competitions.Remove(competition);
        await _context.SaveChangesAsync();
        return true;
    }

    // ===== DELETE REGISTRATION ROUND =====

    public async Task<bool> DeleteRegistrationRoundAsync(int roundId, int competitionId)
    {
        var round = await _context.RegistrationRounds
            .Include(r => r.Registrations)
            .FirstOrDefaultAsync(r => r.RoundId == roundId && r.CompetitionId == competitionId);

        if (round == null) return false;

        var competition = await _context.Competitions
            .Include(c => c.RegistrationRounds)
            .FirstOrDefaultAsync(c => c.CompetitionId == competitionId);

        if (competition != null && DateTime.UtcNow >= competition.StartDate)
            throw new InvalidOperationException("Không thể xóa đợt đăng ký sau khi cuộc thi đã bắt đầu.");

        if (competition != null && competition.RegistrationRounds.Count <= 1)
            throw new InvalidOperationException("Không thể xóa — cuộc thi phải có ít nhất một đợt đăng ký.");

        if (round.Registrations.Any())
            throw new InvalidOperationException(
                $"Đợt \"{round.RoundName}\" đã có {round.Registrations.Count} người đăng ký — không thể xóa.");

        _context.RegistrationRounds.Remove(round);
        await _context.SaveChangesAsync();
        return true;
    }

    // ===== CHANGE STATUS =====

    public async Task<bool> ChangeCompetitionStatusAsync(int competitionId, string newStatus, int organizerId, bool isAdmin = false)
    {
        var competition = await _context.Competitions
            .FirstOrDefaultAsync(c => c.CompetitionId == competitionId && (isAdmin || c.CreatedByUserId == organizerId));

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

    public async Task<OrganizerDashboardViewModel> GetOrganizerDashboardDataAsync(int organizerId, bool isAdmin = false)
    {
        var now = DateTime.UtcNow;

        var competitions = await _context.Competitions
            .Where(c => isAdmin || c.CreatedByUserId == organizerId)
            .Include(c => c.Registrations)
            .Include(c => c.Submissions)
            .Include(c => c.RegistrationRounds)
            .Include(c => c.Teams)
            .ToListAsync();

        var competitionIds = competitions.Select(c => c.CompetitionId).ToList();
        var judges = await _context.Judges
            .Where(j => competitionIds.Contains(j.CompetitionId))
            .ToListAsync();

        // Submission.Status không bao giờ được cập nhật thành "Evaluated" trong luồng chấm điểm
        // thực tế (JudgeController chỉ chuyển "Submitted" -> "Under Review"), nên "đã chấm" phải
        // được xác định qua sự tồn tại của điểm số (Scores) đã được duyệt/không bị từ chối.
        var allSubmissionIds = competitions.SelectMany(c => c.Submissions.Select(s => s.SubmissionId)).ToList();
        var scoredSubmissionIds = allSubmissionIds.Any()
            ? (await _context.Scores
                .Where(s => allSubmissionIds.Contains(s.SubmissionId) && s.ApprovalStatus != "Rejected")
                .Select(s => s.SubmissionId)
                .Distinct()
                .ToListAsync()).ToHashSet()
            : new HashSet<int>();

        // ===== COMPETITION STATUS BREAKDOWN =====
        int totalCompetitions = competitions.Count;
        int draftCompetitions = competitions.Count(c => c.Status == "Draft");
        int completedCompetitions = competitions.Count(c => c.Status == "Completed");

        int openRegistrationCompetitions = competitions.Count(c =>
            c.Status == "Active" && HasActiveRound(c.RegistrationRounds));

        int acceptingSubmissions = competitions.Count(c =>
            c.Status == "Active"
            && !HasActiveRound(c.RegistrationRounds)
            && now < c.SubmissionDeadline);

        int gradingCompetitions = competitions.Count(c =>
            c.Status == "Active" && now >= c.SubmissionDeadline && now < c.EndDate);

        // ===== ACTIVITY METRICS =====
        var activeCompetitions   = competitions.Count(c => c.Status == "Active");
        var pendingRegistrations = competitions.Sum(c => c.Registrations.Count(r => r.Status == "Pending"));
        var totalRegistrations   = competitions.Sum(c => c.Registrations.Count);
        var totalTeams           = competitions.Sum(c => c.Teams.Count);
        var totalSubmissions     = competitions.Sum(c => c.Submissions.Count);
        var evaluatedSubmissions = competitions.Sum(c => c.Submissions.Count(s => scoredSubmissionIds.Contains(s.SubmissionId)));
        var activeJudges         = judges.Count(j => j.Status == "Active");

        // ===== ON-TIME SUBMISSION RATE =====
        var onTimeSubmissions = competitions.Sum(c => c.Submissions.Count(s => s.SubmissionDate <= c.SubmissionDeadline));
        var lateSubmissions   = totalSubmissions - onTimeSubmissions;
        var onTimeSubmissionRate = totalSubmissions > 0
            ? Math.Round((decimal)onTimeSubmissions / totalSubmissions * 100, 1)
            : 0m;
        var urgentCompetitions   = competitions.Count(c =>
            c.Status == "Active" && (c.EndDate - now).TotalDays < 7 && (c.EndDate - now).TotalDays >= 0);
        var scoringCompletionRate = totalSubmissions > 0
            ? Math.Round((decimal)evaluatedSubmissions / totalSubmissions * 100, 1)
            : 0m;

        // ===== COMPETITION PROGRESS TABLE =====
        var progressTable = competitions
            .Where(c => c.Status == "Active" || c.Status == "Draft")
            .OrderBy(c => c.Status == "Draft" ? 1 : 0)
            .ThenBy(c => c.EndDate)
            .Select(c =>
            {
                var activeRound = c.RegistrationRounds
                    .FirstOrDefault(r => now >= r.StartDate && now <= r.EndDate);
                var nextRound = c.RegistrationRounds
                    .Where(r => r.StartDate > now)
                    .OrderBy(r => r.StartDate)
                    .FirstOrDefault();
                var approved   = c.Registrations.Count(r => r.Status == "Approved");
                var pending    = c.Registrations.Count(r => r.Status == "Pending");
                var graded     = c.Submissions.Count(s => scoredSubmissionIds.Contains(s.SubmissionId));
                var total      = c.Submissions.Count;

                return new CompetitionProgressRow
                {
                    CompetitionId       = c.CompetitionId,
                    Name                = c.CompetitionName,
                    Status              = c.Status,
                    Phase               = DetermineCurrentPhase(c),
                    TotalRegistrations  = c.Registrations.Count,
                    PendingRegistrations = pending,
                    ApprovedRegistrations = approved,
                    TotalSubmissions    = total,
                    GradedSubmissions   = graded,
                    GradingProgress     = total > 0 ? Math.Round((decimal)graded / total * 100, 0) : 0,
                    UnderMinParticipants = c.MinParticipants > 0 && approved < c.MinParticipants,
                    MinParticipants     = c.MinParticipants,
                    ActiveRoundName     = activeRound?.RoundName,
                    ActiveRoundEnd      = activeRound?.EndDate,
                    NextRoundStart      = nextRound?.StartDate
                };
            }).ToList();

        // ===== ALERTS =====
        var alerts = new List<AlertItem>();

        // Hồ sơ chờ duyệt
        if (pendingRegistrations > 0)
            alerts.Add(new AlertItem
            {
                Level      = "warning",
                Icon       = "bi-person-exclamation",
                Message    = $"Có {pendingRegistrations} hồ sơ đăng ký đang chờ duyệt.",
                ActionUrl  = "/Organizer/Submissions",
                ActionText = "Xem ngay"
            });

        // Bài chưa chấm xong
        var ungradedCount = totalSubmissions - evaluatedSubmissions;
        if (ungradedCount > 0 && gradingCompetitions > 0)
            alerts.Add(new AlertItem
            {
                Level      = "warning",
                Icon       = "bi-file-earmark-x",
                Message    = $"Còn {ungradedCount} bài thi chưa được chấm điểm.",
                ActionUrl  = "/Organizer/Submissions",
                ActionText = "Phân công chấm"
            });

        // Cuộc thi thiếu người khi đợt đăng ký đang mở sắp đóng (chỉ Active, có round đang mở)
        var underMinNearDeadline = progressTable
            .Where(r => r.Status == "Active"
                        && r.UnderMinParticipants
                        && r.ActiveRoundEnd.HasValue
                        && (r.ActiveRoundEnd.Value - now).TotalDays <= 7)
            .ToList();

        foreach (var comp in underMinNearDeadline)
        {
            var roundEnd  = comp.ActiveRoundEnd!.Value;
            var dLeft     = (roundEnd - now).TotalDays;
            var hLeft     = (roundEnd - now).TotalHours;
            var timeText  = dLeft >= 1 ? $"còn {(int)dLeft} ngày" : $"còn {(int)hLeft} giờ";
            var lvl       = dLeft < 3 ? "danger" : "warning";

            alerts.Add(new AlertItem
            {
                Level      = lvl,
                Icon       = "bi-people",
                Message    = $"\"{comp.Name}\" mới đạt {comp.ApprovedRegistrations}/{comp.MinParticipants} người tối thiểu — đợt \"{comp.ActiveRoundName}\" {timeText} nữa.",
                ActionUrl  = "/Organizer/Contests",
                ActionText = "Xem chi tiết"
            });
        }

        // Deadline khẩn cấp (trong 3 ngày)
        var urgentDeadlines = competitions
            .Where(c => c.Status == "Active")
            .SelectMany(c => c.RegistrationRounds
                .Where(r => r.EndDate > now && (r.EndDate - now).TotalDays < 3)
                .Select(r => new { c.CompetitionName, r.RoundName, r.EndDate }))
            .ToList();

        foreach (var d in urgentDeadlines)
            alerts.Add(new AlertItem
            {
                Level   = "danger",
                Icon    = "bi-alarm",
                Message = $"Đợt đăng ký \"{d.RoundName}\" của \"{d.CompetitionName}\" đóng sau {(int)(d.EndDate - now).TotalHours} giờ."
            });

        // ===== TOP COMPETITIONS =====
        var topCompetitions = competitions
            .OrderByDescending(c => c.Registrations.Count)
            .Take(5)
            .Select(c => new TopCompetitionItem
            {
                CompetitionId    = c.CompetitionId,
                Name             = c.CompetitionName,
                RegistrationCount = c.Registrations.Count,
                Status           = c.Status,
                Phase            = DetermineCurrentPhase(c)
            }).ToList();

        // ===== SỐ LƯỢNG NGƯỜI THAM GIA THEO CUỘC THI (biểu đồ, top 10 để dễ đọc) =====
        var participantsByCompetition = competitions
            .OrderByDescending(c => c.Registrations.Count)
            .Take(10)
            .Select(c => new CompetitionParticipantsItem
            {
                CompetitionId     = c.CompetitionId,
                Name              = c.CompetitionName,
                RegistrationCount = c.Registrations.Count,
                ApprovedCount     = c.Registrations.Count(r => r.Status == "Approved")
            }).ToList();

        // ===== PHÂN BỐ ĐIỂM SỐ (theo % so với điểm tối đa của vòng thi tương ứng) =====
        var scoreDistribution = new ScoreDistributionData();
        if (allSubmissionIds.Any())
        {
            var scoresForDist = await _context.Scores
                .Where(s => allSubmissionIds.Contains(s.SubmissionId) && s.ApprovalStatus != "Rejected")
                .ToListAsync();

            var roundsForCriteria = await _context.CompetitionRounds
                .Include(r => r.ScoringCriteria)
                .Where(r => competitionIds.Contains(r.CompetitionId))
                .ToListAsync();
            var maxPossibleByRound = roundsForCriteria.ToDictionary(
                r => r.RoundId,
                r => r.ScoringCriteria.Any() ? r.ScoringCriteria.Sum(c => c.MaxScore) : 100m);

            var submissionRoundMap = competitions
                .SelectMany(c => c.Submissions)
                .ToDictionary(s => s.SubmissionId, s => s.CompetitionRoundId);

            foreach (var group in scoresForDist.GroupBy(s => s.SubmissionId))
            {
                submissionRoundMap.TryGetValue(group.Key, out var roundId);
                decimal maxPossible = roundId.HasValue && maxPossibleByRound.TryGetValue(roundId.Value, out var mp)
                    ? mp : 100m;
                if (maxPossible <= 0) continue;

                var criteriaAvgs = group.GroupBy(s => s.CriteriaId).Select(g => g.Average(s => s.Score));
                decimal total = criteriaAvgs.Sum();
                decimal pct = total / maxPossible * 100;

                if (pct < 50) scoreDistribution.Below50++;
                else if (pct < 65) scoreDistribution.From50To65++;
                else if (pct < 80) scoreDistribution.From65To80++;
                else if (pct < 90) scoreDistribution.From80To90++;
                else scoreDistribution.Above90++;
            }
        }

        // ===== PROGRESS CHART (4 tuần gần nhất, cuộc thi Active đầu tiên) =====
        var progressData      = new List<CompetitionProgressData>();
        var activeCompetition = competitions.FirstOrDefault(c => c.Status == "Active");
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

        // ===== APPROVAL RATIO =====
        var approvalRatio = new ApprovalRatioData
        {
            ApprovedCount = competitions.Sum(c => c.Registrations.Count(r => r.Status == "Approved")),
            PendingCount  = pendingRegistrations,
            RejectedCount = competitions.Sum(c => c.Registrations.Count(r => r.Status == "Rejected"))
        };

        // ===== DEADLINES (dựa trên RegistrationRounds) =====
        var deadlines = new List<DeadlineItem>();
        foreach (var comp in competitions.Where(c => c.Status == "Active" || c.Status == "Draft"))
        {
            // Từng round đang mở hoặc sắp mở
            foreach (var round in comp.RegistrationRounds.OrderBy(r => r.EndDate))
            {
                if (round.EndDate <= now) continue;

                var daysUntil = (round.EndDate - now).TotalDays;
                var isActive  = now >= round.StartDate;
                var label     = isActive ? $"Đóng đợt \"{round.RoundName}\"" : $"Mở đợt \"{round.RoundName}\"";
                var date      = isActive ? round.EndDate : round.StartDate;
                var days2     = (date - now).TotalDays;

                deadlines.Add(new DeadlineItem
                {
                    CompetitionId   = comp.CompetitionId,
                    CompetitionName = comp.CompetitionName,
                    Title           = label,
                    DeadlineDate    = date,
                    Status          = days2 < 3 ? "urgent" : days2 < 7 ? "warning" : "normal",
                    Description     = $"{comp.CompetitionName} — Đã nhận {comp.Registrations.Count} hồ sơ."
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
                    Description     = $"{comp.CompetitionName} — Đã nhận {comp.Submissions.Count} bài thi."
                });
            }

            if (comp.EndDate > now && comp.Submissions.Count > 0)
            {
                var graded      = comp.Submissions.Count(s => scoredSubmissionIds.Contains(s.SubmissionId));
                var pct         = (decimal)graded / comp.Submissions.Count * 100;
                var daysUntil   = (comp.EndDate - now).TotalDays;
                deadlines.Add(new DeadlineItem
                {
                    CompetitionId      = comp.CompetitionId,
                    CompetitionName    = comp.CompetitionName,
                    Title              = "Hạn nộp điểm Giám khảo",
                    DeadlineDate       = comp.EndDate,
                    Status             = daysUntil < 3 ? "urgent" : daysUntil < 7 ? "warning" : "normal",
                    ProgressPercentage = Math.Round(pct, 0),
                    Description        = $"{comp.CompetitionName} — Đã chấm {graded}/{comp.Submissions.Count} bài."
                });
            }
        }

        deadlines = deadlines.OrderBy(d => d.DeadlineDate).ToList();

        // ===== RECENT ACTIVITIES =====
        var recentActivities = new List<ActivityLog>();

        var recentScores = await _context.Scores
            .Include(s => s.Submission)
            .Where(s => s.Submission != null && competitionIds.Contains(s.Submission.CompetitionId))
            .OrderByDescending(s => s.ScoredDate)
            .Take(5)
            .ToListAsync();

        foreach (var score in recentScores)
            recentActivities.Add(new ActivityLog
            {
                Type        = "score",
                Title       = "Giám khảo chấm bài hoàn tất",
                Description = $"Bài thi #{score.SubmissionId} đã được chấm.",
                CreatedAt   = score.ScoredDate,
                UserName    = "Giám khảo"
            });

        var recentRegistrations = await _context.Registrations
            .Include(r => r.User)
            .Where(r => r.Status == "Pending" && competitionIds.Contains(r.CompetitionId))
            .OrderByDescending(r => r.RegistrationDate)
            .Take(3)
            .ToListAsync();

        foreach (var reg in recentRegistrations)
            recentActivities.Add(new ActivityLog
            {
                Type        = "registration",
                Title       = "Hồ sơ đăng ký mới chờ duyệt",
                Description = $"Sinh viên {reg.User?.FullName ?? "Unknown"} đã nộp hồ sơ.",
                CreatedAt   = reg.RegistrationDate,
                UserName    = reg.User?.FullName
            });

        var recentSubmissions = await _context.Submissions
            .Where(s => competitionIds.Contains(s.CompetitionId))
            .OrderByDescending(s => s.SubmissionDate)
            .Take(2)
            .ToListAsync();

        foreach (var sub in recentSubmissions)
            recentActivities.Add(new ActivityLog
            {
                Type        = "submission",
                Title       = "Bài dự thi mới được nộp",
                Description = $"Bài thi #{sub.SubmissionId} đã được nộp.",
                CreatedAt   = sub.SubmissionDate
            });

        recentActivities = recentActivities.OrderByDescending(a => a.CreatedAt).ToList();

        return new OrganizerDashboardViewModel
        {
            TotalCompetitions            = totalCompetitions,
            DraftCompetitions            = draftCompetitions,
            OpenRegistrationCompetitions = openRegistrationCompetitions,
            AcceptingSubmissions         = acceptingSubmissions,
            GradingCompetitions          = gradingCompetitions,
            CompletedCompetitions        = completedCompetitions,
            ActiveCompetitions           = activeCompetitions,
            PendingRegistrations         = pendingRegistrations,
            TotalRegistrations           = totalRegistrations,
            TotalTeams                   = totalTeams,
            TotalSubmissions             = totalSubmissions,
            EvaluatedSubmissions         = evaluatedSubmissions,
            ActiveJudges                 = activeJudges,
            UrgentCompetitions           = urgentCompetitions,
            ScoringCompletionRate        = scoringCompletionRate,
            OnTimeSubmissions            = onTimeSubmissions,
            LateSubmissions              = lateSubmissions,
            OnTimeSubmissionRate         = onTimeSubmissionRate,
            ScoreDistribution            = scoreDistribution,
            ParticipantsByCompetition    = participantsByCompetition,
            Alerts                       = alerts,
            CompetitionProgressTable     = progressTable,
            TopCompetitionsByRegistrations = topCompetitions,
            ProgressData                 = progressData,
            ApprovalRatio                = approvalRatio,
            UpcomingDeadlines            = deadlines,
            RecentActivities             = recentActivities
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
        { "pdf" };

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

    // ===== COMPETITION ROUNDS (VÒNG THI) =====

    public async Task<bool> AddCompetitionRoundAsync(int competitionId, CompetitionRoundCreateDto dto)
    {
        var competition = await _context.Competitions
            .Include(c => c.CompetitionRounds)
            .FirstOrDefaultAsync(c => c.CompetitionId == competitionId);
        if (competition == null) return false;

        var startDate = ToUtc(dto.StartDate);
        var endDate   = ToUtc(dto.EndDate);

        if (endDate <= startDate)
            throw new InvalidOperationException("Ngày kết thúc phải sau ngày bắt đầu.");
        if (startDate < competition.SubmissionDeadline)
            throw new InvalidOperationException("Vòng thi phải bắt đầu sau hạn nộp bài.");
        if (endDate > competition.EndDate)
            throw new InvalidOperationException("Vòng thi phải kết thúc trước ngày kết thúc cuộc thi.");

        var nextOrder = competition.CompetitionRounds.Any()
            ? competition.CompetitionRounds.Max(r => r.RoundOrder) + 1 : 1;

        var round = new CompetitionRounds
        {
            CompetitionId     = competitionId,
            RoundName         = dto.RoundName,
            RoundOrder        = nextOrder,
            Description       = dto.Description,
            StartDate         = startDate,
            EndDate           = endDate,
            SubmissionDeadline = dto.SubmissionDeadline.HasValue ? ToUtc(dto.SubmissionDeadline.Value) : null,
            MaxAdvancing      = dto.MaxAdvancing,
            RequiresFile      = dto.RequiresFile,
            RequiresVideo     = dto.RequiresVideo,
            RequiresOtherLink = dto.RequiresOtherLink,
            IsOnline          = dto.IsOnline,
            MeetingLink       = dto.MeetingLink,
            MeetingTime       = dto.MeetingTime.HasValue ? ToUtc(dto.MeetingTime.Value) : null,
            Location          = dto.Location,
            Status            = "Upcoming",
            CreatedAt         = DateTime.UtcNow
        };
        _context.CompetitionRounds.Add(round);
        await _context.SaveChangesAsync();

        if (dto.ScoringCriteria != null && dto.ScoringCriteria.Any())
        {
            foreach (var c in dto.ScoringCriteria)
            {
                // Chuẩn hóa weight: nếu được gửi dạng % (> 1) thì chuyển sang phân số
                if (c.Weight > 1m)
                    c.Weight = c.Weight > 100m ? c.Weight / 10000m : c.Weight / 100m;

                _context.ScoringCriteria.Add(new ScoringCriteria
                {
                    RoundId      = round.RoundId,
                    CriteriaName = c.CriteriaName,
                    Description  = c.Description,
                    MaxScore     = c.MaxScore,
                    Weight       = c.Weight,
                    Order        = c.Order,
                    CreatedAt    = DateTime.UtcNow
                });
            }
            await _context.SaveChangesAsync();
        }
        return true;
    }

    public async Task<bool> UpdateCompetitionRoundAsync(int roundId, int competitionId, UpdateCompetitionRoundDto dto)
    {
        var round = await _context.CompetitionRounds
            .FirstOrDefaultAsync(r => r.RoundId == roundId && r.CompetitionId == competitionId);
        if (round == null) return false;

        var competition = await _context.Competitions
            .FirstOrDefaultAsync(c => c.CompetitionId == competitionId);
        if (competition == null) return false;

        var startDate = ToUtc(dto.StartDate);
        var endDate   = ToUtc(dto.EndDate);

        if (endDate <= startDate)
            throw new InvalidOperationException("Ngày kết thúc phải sau ngày bắt đầu.");
        if (startDate < competition.SubmissionDeadline)
            throw new InvalidOperationException("Vòng thi phải bắt đầu sau hạn nộp bài.");
        if (endDate > competition.EndDate)
            throw new InvalidOperationException("Vòng thi phải kết thúc trước ngày kết thúc cuộc thi.");

        round.RoundName         = dto.RoundName;
        round.Description       = dto.Description;
        round.StartDate         = startDate;
        round.EndDate           = endDate;
        round.SubmissionDeadline = dto.SubmissionDeadline.HasValue ? ToUtc(dto.SubmissionDeadline.Value) : null;
        round.MaxAdvancing      = dto.MaxAdvancing;
        round.RequiresFile      = dto.RequiresFile;
        round.RequiresVideo     = dto.RequiresVideo;
        round.RequiresOtherLink = dto.RequiresOtherLink;
        round.IsOnline          = dto.IsOnline;
        round.MeetingLink       = dto.MeetingLink;
        round.MeetingTime       = dto.MeetingTime.HasValue ? ToUtc(dto.MeetingTime.Value) : null;
        round.Location          = dto.Location;
        round.UpdatedAt         = DateTime.UtcNow;

        // Cập nhật tiêu chí chấm điểm — ghi đè toàn bộ nếu danh sách được gửi
        if (dto.ScoringCriteria != null && dto.ScoringCriteria.Any())
        {
            var existing = await _context.ScoringCriteria.Where(sc => sc.RoundId == roundId).ToListAsync();
            _context.ScoringCriteria.RemoveRange(existing);
            foreach (var c in dto.ScoringCriteria)
            {
                if (c.Weight > 1m)
                    c.Weight = c.Weight > 100m ? c.Weight / 10000m : c.Weight / 100m;

                _context.ScoringCriteria.Add(new ScoringCriteria
                {
                    RoundId      = roundId,
                    CriteriaName = c.CriteriaName,
                    Description  = c.Description,
                    MaxScore     = c.MaxScore,
                    Weight       = c.Weight,
                    Order        = c.Order,
                    CreatedAt    = DateTime.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteCompetitionRoundAsync(int roundId, int competitionId)
    {
        var round = await _context.CompetitionRounds
            .FirstOrDefaultAsync(r => r.RoundId == roundId && r.CompetitionId == competitionId);
        if (round == null) return false;

        _context.CompetitionRounds.Remove(round);
        await _context.SaveChangesAsync();
        return true;
    }

    // ─── Registration Form Fields ─────────────────────────────────────────────

    public async Task<List<FormFieldReadDto>> GetFormFieldsAsync(int competitionId)
    {
        return await _context.RegistrationFormFields
            .Where(f => f.CompetitionId == competitionId)
            .OrderBy(f => f.DisplayOrder)
            .ThenBy(f => f.FieldId)
            .Select(f => new FormFieldReadDto
            {
                FieldId      = f.FieldId,
                FieldLabel   = f.FieldLabel,
                FieldType    = f.FieldType,
                IsRequired   = f.IsRequired,
                Placeholder  = f.Placeholder,
                Options      = f.Options,
                HelpText     = f.HelpText,
                DisplayOrder = f.DisplayOrder,
                IsActive     = f.IsActive
            })
            .ToListAsync();
    }

    public async Task<(bool ok, string message, int fieldId)> AddFormFieldAsync(int competitionId, FormFieldCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FieldLabel))
            return (false, "Nhãn trường không được để trống.", 0);

        var validTypes = new[] { "Text", "Textarea", "Select", "Checkbox", "File", "Date", "Number", "Email" };
        if (!validTypes.Contains(dto.FieldType))
            return (false, "Loại trường không hợp lệ.", 0);

        var field = new RegistrationFormFields
        {
            CompetitionId = competitionId,
            FieldLabel    = dto.FieldLabel.Trim(),
            FieldType     = dto.FieldType,
            IsRequired    = dto.IsRequired,
            Placeholder   = dto.Placeholder?.Trim(),
            Options       = dto.Options,
            HelpText      = dto.HelpText?.Trim(),
            DisplayOrder  = dto.DisplayOrder,
            IsActive      = dto.IsActive,
            CreatedAt     = DateTime.UtcNow
        };

        _context.RegistrationFormFields.Add(field);
        await _context.SaveChangesAsync();
        return (true, "Thêm trường thành công!", field.FieldId);
    }

    public async Task<(bool ok, string message)> UpdateFormFieldAsync(int fieldId, int competitionId, FormFieldCreateDto dto)
    {
        var field = await _context.RegistrationFormFields
            .FirstOrDefaultAsync(f => f.FieldId == fieldId && f.CompetitionId == competitionId);
        if (field == null) return (false, "Trường không tìm thấy.");

        // Đã có người đăng ký: chỉ cho phép đổi trạng thái ẩn/hiện, không cho sửa thông tin trường
        // (dữ liệu thí sinh đã điền gắn theo FieldId, sửa cấu trúc có thể làm sai lệch dữ liệu cũ)
        if (await HasRegistrationsAsync(competitionId))
        {
            field.IsActive = dto.IsActive;
            await _context.SaveChangesAsync();
            return (true, "Đã cập nhật trạng thái hiển thị của trường!");
        }

        if (string.IsNullOrWhiteSpace(dto.FieldLabel))
            return (false, "Nhãn trường không được để trống.");

        var validTypes = new[] { "Text", "Textarea", "Select", "Checkbox", "File", "Date", "Number", "Email" };
        if (!validTypes.Contains(dto.FieldType))
            return (false, "Loại trường không hợp lệ.");

        field.FieldLabel   = dto.FieldLabel.Trim();
        field.FieldType    = dto.FieldType;
        field.IsRequired   = dto.IsRequired;
        field.Placeholder  = dto.Placeholder?.Trim();
        field.Options      = dto.Options;
        field.HelpText     = dto.HelpText?.Trim();
        field.DisplayOrder = dto.DisplayOrder;
        field.IsActive     = dto.IsActive;

        await _context.SaveChangesAsync();
        return (true, "Cập nhật trường thành công!");
    }

    public async Task<(bool ok, string message)> DeleteFormFieldAsync(int fieldId, int competitionId)
    {
        var field = await _context.RegistrationFormFields
            .FirstOrDefaultAsync(f => f.FieldId == fieldId && f.CompetitionId == competitionId);
        if (field == null) return (false, "Trường không tìm thấy.");

        if (await HasRegistrationsAsync(competitionId))
            return (false, "Cuộc thi đã có người đăng ký — không thể xóa trường này. Bạn có thể ẩn trường thay vì xóa.");

        _context.RegistrationFormFields.Remove(field);
        await _context.SaveChangesAsync();
        return (true, "Đã xóa trường.");
    }

    // Cùng điều kiện với "canEditFormStructure" phía Edit.cshtml (khác Withdrawn/Rejected)
    private async Task<bool> HasRegistrationsAsync(int competitionId)
    {
        return await _context.Registrations.AnyAsync(r => r.CompetitionId == competitionId
            && r.Status != "Withdrawn" && r.Status != "Rejected");
    }

    public async Task<(bool ok, string message)> ReorderFormFieldsAsync(int competitionId, List<int> orderedFieldIds)
    {
        var fields = await _context.RegistrationFormFields
            .Where(f => f.CompetitionId == competitionId && orderedFieldIds.Contains(f.FieldId))
            .ToListAsync();

        for (int i = 0; i < orderedFieldIds.Count; i++)
        {
            var field = fields.FirstOrDefault(f => f.FieldId == orderedFieldIds[i]);
            if (field != null) field.DisplayOrder = i;
        }

        await _context.SaveChangesAsync();
        return (true, "Đã cập nhật thứ tự.");
    }
}
