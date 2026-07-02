using Web_cham_diem.Models;
using Web_cham_diem.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Web_cham_diem.Services;

public class GradingService : IGradingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GradingService> _logger;

    public GradingService(ApplicationDbContext context, ILogger<GradingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<OrganizerGradingViewModel> GetGradingViewAsync(int organizerId, int? competitionId)
    {
        var competitions = await _context.Competitions
            .Where(c => c.CreatedByUserId == organizerId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CompetitionBasicDto
            {
                CompetitionId = c.CompetitionId,
                CompetitionName = c.CompetitionName
            })
            .ToListAsync();

        if (!competitionId.HasValue && competitions.Any())
            competitionId = competitions.First().CompetitionId;

        var vm = new OrganizerGradingViewModel
        {
            SelectedCompetitionId = competitionId,
            Competitions = competitions
        };

        if (!competitionId.HasValue) return vm;
        var cid = competitionId.Value;

        // Load competition rounds
        var rounds = await _context.CompetitionRounds
            .Where(r => r.CompetitionId == cid)
            .OrderBy(r => r.RoundOrder)
            .Select(r => new CompetitionRoundBasicDto
            {
                RoundId    = r.RoundId,
                RoundName  = r.RoundName,
                RoundOrder = r.RoundOrder,
                StartDate  = r.StartDate,
                EndDate    = r.EndDate,
                Status     = r.Status
            })
            .ToListAsync();
        vm.CompetitionRounds = rounds;

        // Load judges (kèm round để lấy tên vòng)
        var judges = await _context.Judges
            .Include(j => j.User)
            .Include(j => j.Round)
            .Where(j => j.CompetitionId == cid)
            .ToListAsync();

        // Load all assignments with related data
        var allAssignments = await _context.JudgeAssignments
            .Include(a => a.Judge).ThenInclude(j => j.User)
            .Include(a => a.Submission).ThenInclude(s => s.Registration).ThenInclude(r => r!.User)
            .Include(a => a.Submission).ThenInclude(s => s.Team)
            .Where(a => a.CompetitionId == cid)
            .ToListAsync();

        var now = DateTime.UtcNow;
        var activeAssigns = allAssignments.Where(a => a.Status != "Reassigned").ToList();

        // Stats by judge
        var statsByJudge = activeAssigns
            .GroupBy(a => a.JudgeId)
            .ToDictionary(g => g.Key, g => new
            {
                Total      = g.Count(),
                Completed  = g.Count(a => a.Status == "Completed"),
                InProgress = g.Count(a => a.Status == "InProgress"),
                Items      = g.ToList()
            });

        vm.Judges = judges.Select(j =>
        {
            statsByJudge.TryGetValue(j.JudgeId, out var s);
            int total = s?.Total ?? 0, done = s?.Completed ?? 0;
            return new JudgeOverviewDto
            {
                JudgeId        = j.JudgeId,
                UserId         = j.UserId,
                FullName       = j.User.FullName,
                Email          = j.User.Email,
                Expertise      = j.Expertise,
                JudgeRole      = j.JudgeRole,
                Status         = j.Status,
                AssignedDate   = j.AssignedDate,
                TotalAssigned  = total,
                Completed      = done,
                InProgress     = s?.InProgress ?? 0,
                CompletionRate = total > 0 ? Math.Round(done * 100.0 / total, 1) : 0,
                RoundId        = j.RoundId,
                RoundName      = j.Round?.RoundName,
                RoundOrder     = j.Round?.RoundOrder ?? int.MaxValue
            };
        })
        .OrderBy(j => j.RoundOrder)
        .ThenBy(j => j.JudgeRole == "HeadJudge" ? 0 : j.JudgeRole == "ViceHead" ? 1 : 2)
        .ThenBy(j => j.FullName)
        .ToList();

        // Submissions with assignment info
        var submissions = await _context.Submissions
            .Include(s => s.Registration).ThenInclude(r => r!.User)
            .Include(s => s.Team)
            .Where(s => s.CompetitionId == cid)
            .ToListAsync();

        var assignsBySubmission = activeAssigns
            .GroupBy(a => a.SubmissionId)
            .ToDictionary(g => g.Key, g => g.ToList());

        vm.Submissions = submissions.Select(s =>
        {
            assignsBySubmission.TryGetValue(s.SubmissionId, out var assigns);
            return new SubmissionForAssignmentDto
            {
                SubmissionId         = s.SubmissionId,
                Title                = !string.IsNullOrEmpty(s.Title) ? s.Title : "Bài #" + s.SubmissionId,
                TeamOrRepresentative = s.Team?.TeamName ?? s.Registration?.User?.FullName ?? "Unknown",
                SubmissionStatus     = s.Status,
                CompetitionRoundId   = s.CompetitionRoundId,
                CurrentAssignments   = assigns?.Select(a => new AssignmentInfoDto
                {
                    AssignmentId    = a.AssignmentId,
                    JudgeId         = a.JudgeId,
                    JudgeName       = a.Judge.User.FullName,
                    Status          = a.Status,
                    GradingDeadline = a.GradingDeadline
                }).ToList() ?? new()
            };
        }).ToList();

        // Assignment detail list
        vm.Assignments = activeAssigns.Select(a => new AssignmentDetailDto
        {
            AssignmentId   = a.AssignmentId,
            JudgeId        = a.JudgeId,
            JudgeName      = a.Judge.User.FullName,
            JudgeRole      = a.Judge.JudgeRole,
            SubmissionId   = a.SubmissionId,
            SubmissionTitle= !string.IsNullOrEmpty(a.Submission.Title) ? a.Submission.Title : "Bài #" + a.SubmissionId,
            TeamOrRep      = a.Submission.Team?.TeamName ?? a.Submission.Registration?.User?.FullName ?? "Unknown",
            Status         = a.Status,
            AssignedDate   = a.AssignedDate,
            GradingDeadline= a.GradingDeadline,
            IsOverdue      = a.GradingDeadline.HasValue && now > a.GradingDeadline.Value && a.Status != "Completed",
            CompletedAt    = a.CompletedAt
        })
        .OrderBy(a => a.IsOverdue ? 0 : a.Status == "Completed" ? 2 : 1)
        .ThenByDescending(a => a.AssignedDate)
        .ToList();

        // Scoring criteria (theo từng vòng thuộc cuộc thi)
        vm.ScoringCriteria = await _context.ScoringCriteria
            .Where(c => c.Round.CompetitionId == cid)
            .OrderBy(c => c.Round.RoundOrder)
            .ThenBy(c => c.Order)
            .Select(c => new ScoringCriteriaViewDto
            {
                CriteriaId   = c.CriteriaId,
                CriteriaName = c.CriteriaName,
                Description  = c.Description,
                MaxScore     = c.MaxScore,
                Weight       = c.Weight,
                Order        = c.Order
            })
            .ToListAsync();

        // Alerts: judges with overdue, low progress, or tight deadline
        vm.Alerts = vm.Judges
            .Where(j => j.TotalAssigned > 0)
            .Select(j =>
            {
                statsByJudge.TryGetValue(j.JudgeId, out var st);
                var nearestDeadline = st?.Items
                    .Where(a => a.Status != "Completed" && a.GradingDeadline.HasValue)
                    .Select(a => a.GradingDeadline)
                    .OrderBy(d => d)
                    .FirstOrDefault();
                bool hasDeadline = nearestDeadline.HasValue;
                int hours = hasDeadline ? (int)(nearestDeadline!.Value - now).TotalHours : int.MaxValue;
                return new JudgeAlertDto
                {
                    JudgeId              = j.JudgeId,
                    JudgeName            = j.FullName,
                    TotalAssigned        = j.TotalAssigned,
                    Completed            = j.Completed,
                    CompletionRate       = j.CompletionRate,
                    NearestDeadline      = nearestDeadline,
                    HoursUntilDeadline   = hours,
                    IsOverdue            = hasDeadline && hours < 0
                };
            })
            .Where(a => a.IsOverdue || a.CompletionRate < 50 || (a.HoursUntilDeadline >= 0 && a.HoursUntilDeadline < 48))
            .OrderBy(a => a.HoursUntilDeadline)
            .ToList();

        // Judge pool (global, independent of competition)
        vm.JudgePool = await GetJudgePoolAsync();

        // Overall stats
        vm.ActiveJudges         = vm.Judges.Count(j => j.Status == "Active");
        vm.TotalAssignments     = activeAssigns.Count;
        vm.CompletedAssignments = activeAssigns.Count(a => a.Status == "Completed");
        vm.CompletionRate       = vm.TotalAssignments > 0
            ? Math.Round(vm.CompletedAssignments * 100.0 / vm.TotalAssignments, 1)
            : 0;

        return vm;
    }

    public async Task<List<UserSearchDto>> SearchUsersForJudgeAsync(int competitionId, string search, int? roundId = null)
    {
        // Chỉ loại trừ user đã là giám khảo trong cùng vòng thi đó
        var existingQuery = _context.Judges.Where(j => j.CompetitionId == competitionId);
        if (roundId.HasValue)
            existingQuery = existingQuery.Where(j => j.RoundId == roundId.Value);
        var existingIds = await existingQuery.Select(j => j.UserId).ToListAsync();

        return await _context.Users
            .Where(u => u.IsActive
                && !existingIds.Contains(u.UserId)
                && (u.FullName.Contains(search) || u.Email.Contains(search)))
            .Take(8)
            .Select(u => new UserSearchDto
            {
                UserId    = u.UserId,
                FullName  = u.FullName,
                Email     = u.Email,
                StudentId = u.StudentId
            })
            .ToListAsync();
    }

    public async Task<(bool success, string message)> AddJudgeAsync(int competitionId, AddJudgeDto dto)
    {
        if (await _context.Competitions.FindAsync(competitionId) == null)
            return (false, "Cuộc thi không tìm thấy.");

        var user = await _context.Users.FindAsync(dto.UserId);
        if (user == null) return (false, "Người dùng không tìm thấy.");

        // Kiểm tra nếu có RoundId thì round phải thuộc cuộc thi này
        if (dto.RoundId.HasValue)
        {
            bool roundExists = await _context.CompetitionRounds
                .AnyAsync(r => r.RoundId == dto.RoundId.Value && r.CompetitionId == competitionId);
            if (!roundExists) return (false, "Vòng thi không thuộc cuộc thi này.");
        }

        // Không cho thêm cùng người vào cùng vòng 2 lần (nếu không có vòng: không cho thêm 2 lần vào cùng cuộc thi)
        bool duplicate = dto.RoundId.HasValue
            ? await _context.Judges.AnyAsync(j => j.CompetitionId == competitionId && j.UserId == dto.UserId && j.RoundId == dto.RoundId)
            : await _context.Judges.AnyAsync(j => j.CompetitionId == competitionId && j.UserId == dto.UserId && j.RoundId == null);
        if (duplicate)
        {
            string where = dto.RoundId.HasValue ? "vòng thi này" : "cuộc thi (không gán vòng)";
            return (false, $"Người này đã là giám khảo trong {where}.");
        }

        // Mỗi vòng chỉ có 1 Trưởng ban
        if (dto.Role == "HeadJudge")
        {
            var headQuery = _context.Judges
                .Where(j => j.CompetitionId == competitionId && j.JudgeRole == "HeadJudge" && j.Status == "Active");
            headQuery = dto.RoundId.HasValue
                ? headQuery.Where(j => j.RoundId == dto.RoundId)
                : headQuery.Where(j => j.RoundId == null);
            if (await headQuery.AnyAsync())
            {
                string where = dto.RoundId.HasValue ? "Vòng thi này" : "Cuộc thi (chưa gán vòng)";
                return (false, $"{where} đã có Trưởng ban. Mỗi vòng chỉ 1 Trưởng ban.");
            }
        }

        _context.Judges.Add(new Judges
        {
            UserId        = dto.UserId,
            CompetitionId = competitionId,
            RoundId       = dto.RoundId,
            JudgeRole     = dto.Role,
            Expertise     = dto.Expertise,
            Status        = "Active",
            AssignedDate  = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        string roundLabel = dto.RoundId.HasValue
            ? (await _context.CompetitionRounds.Where(r => r.RoundId == dto.RoundId).Select(r => r.RoundName).FirstOrDefaultAsync() ?? "")
            : "";
        string suffix = string.IsNullOrEmpty(roundLabel) ? "" : $" — vòng {roundLabel}";
        return (true, $"Đã thêm {user.FullName} ({RoleText(dto.Role)}){suffix}.");
    }

    public async Task<(bool success, string message)> RemoveJudgeAsync(int judgeId, int competitionId)
    {
        var judge = await _context.Judges
            .FirstOrDefaultAsync(j => j.JudgeId == judgeId && j.CompetitionId == competitionId);
        if (judge == null) return (false, "Giám khảo không tìm thấy.");

        bool hasPending = await _context.JudgeAssignments
            .AnyAsync(a => a.JudgeId == judgeId && a.Status != "Completed" && a.Status != "Reassigned");
        if (hasPending) return (false, "Giám khảo còn bài chưa chấm xong. Thu hồi phân công trước.");

        _context.Judges.Remove(judge);
        await _context.SaveChangesAsync();
        return (true, "Đã xóa giám khảo khỏi ban.");
    }

    public async Task<(bool success, string message)> UpdateJudgeRoleAsync(int judgeId, int competitionId, UpdateJudgeRoleDto dto)
    {
        var judge = await _context.Judges
            .FirstOrDefaultAsync(j => j.JudgeId == judgeId && j.CompetitionId == competitionId);
        if (judge == null) return (false, "Giám khảo không tìm thấy.");

        if (dto.Role == "HeadJudge")
        {
            var headQuery = _context.Judges
                .Where(j => j.CompetitionId == competitionId && j.JudgeId != judgeId
                            && j.JudgeRole == "HeadJudge" && j.Status == "Active");
            headQuery = judge.RoundId.HasValue
                ? headQuery.Where(j => j.RoundId == judge.RoundId)
                : headQuery.Where(j => j.RoundId == null);
            if (await headQuery.AnyAsync())
                return (false, "Vòng thi này đã có Trưởng ban. Không thể đặt thêm.");
        }

        judge.JudgeRole  = dto.Role;
        judge.Expertise  = dto.Expertise;
        judge.UpdatedAt  = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return (true, "Cập nhật thông tin giám khảo thành công.");
    }

    public async Task<(bool success, string message)> AssignSubmissionsAsync(int competitionId, AssignSubmissionsDto dto, int assignedByUserId)
    {
        if (!await _context.Judges.AnyAsync(j => j.JudgeId == dto.JudgeId && j.CompetitionId == competitionId))
            return (false, "Giám khảo không thuộc cuộc thi này.");

        int added = 0, skipped = 0;
        foreach (var subId in dto.SubmissionIds.Distinct())
        {
            var existing = await _context.JudgeAssignments
                .FirstOrDefaultAsync(a => a.JudgeId == dto.JudgeId && a.SubmissionId == subId);

            if (existing != null)
            {
                // Đang có phân công active → bỏ qua
                if (existing.Status != "Reassigned") { skipped++; continue; }

                // Phân công cũ đã bị hủy → kích hoạt lại thay vì INSERT mới (tránh vi phạm unique index)
                existing.Status           = "Pending";
                existing.RoundId          = dto.RoundId ?? existing.RoundId;
                existing.GradingDeadline  = dto.GradingDeadline?.ToUniversalTime();
                existing.AssignedDate     = DateTime.UtcNow;
                existing.AssignedByUserId = assignedByUserId > 0 ? assignedByUserId : 1;
                existing.CompletedAt      = null;
                existing.UpdatedAt        = DateTime.UtcNow;
            }
            else
            {
                _context.JudgeAssignments.Add(new JudgeAssignments
                {
                    JudgeId          = dto.JudgeId,
                    SubmissionId     = subId,
                    CompetitionId    = competitionId,
                    RoundId          = dto.RoundId,
                    AssignedByUserId = assignedByUserId > 0 ? assignedByUserId : 1,
                    GradingDeadline  = dto.GradingDeadline?.ToUniversalTime(),
                    Status           = "Pending",
                    AssignedDate     = DateTime.UtcNow
                });
            }
            added++;
        }
        await _context.SaveChangesAsync();
        string msg = $"Đã phân công {added} bài thành công.";
        if (skipped > 0) msg += $" Bỏ qua {skipped} bài đã phân công trước.";
        return (true, msg);
    }

    public async Task<(bool success, string message)> RevokeAssignmentAsync(int assignmentId, int competitionId)
    {
        var a = await _context.JudgeAssignments
            .FirstOrDefaultAsync(x => x.AssignmentId == assignmentId && x.CompetitionId == competitionId);
        if (a == null) return (false, "Phân công không tìm thấy.");
        if (a.Status == "Completed") return (false, "Không thể thu hồi phân công đã hoàn thành.");

        a.Status    = "Reassigned";
        a.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return (true, "Đã thu hồi phân công.");
    }

    public async Task<List<JudgePoolItemDto>> GetJudgePoolAsync()
    {
        var judgeIds = await _context.UserRoles
            .Where(ur => ur.RoleId == 4)
            .Select(ur => ur.UserId)
            .ToListAsync();

        var users = await _context.Users
            .Where(u => judgeIds.Contains(u.UserId) && u.IsActive)
            .OrderBy(u => u.FullName)
            .ToListAsync();

        var allJudges = await _context.Judges
            .Include(j => j.Competition)
            .Include(j => j.Round)
            .Where(j => judgeIds.Contains(j.UserId))
            .ToListAsync();

        return users.Select(u =>
        {
            var assignments = allJudges
                .Where(j => j.UserId == u.UserId && j.RoundId.HasValue && j.Round != null)
                .OrderBy(j => j.Round!.StartDate)
                .Select(j => new JudgeRoundSummaryDto
                {
                    JudgeId         = j.JudgeId,
                    CompetitionId   = j.CompetitionId,
                    CompetitionName = j.Competition?.CompetitionName ?? "",
                    RoundId         = j.RoundId!.Value,
                    RoundName       = j.Round!.RoundName,
                    JudgeRole       = j.JudgeRole,
                    StartDate       = j.Round!.StartDate,
                    EndDate         = j.Round!.EndDate
                }).ToList();

            return new JudgePoolItemDto
            {
                UserId             = u.UserId,
                FullName           = u.FullName,
                Email              = u.Email,
                StudentId          = u.StudentId,
                TotalRoundsAssigned = assignments.Count,
                RoundAssignments   = assignments
            };
        }).ToList();
    }

    public async Task<(bool success, string message)> AddUserToJudgePoolAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return (false, "Người dùng không tìm thấy.");

        bool already = await _context.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.RoleId == 4);
        if (already) return (false, $"{user.FullName} đã là giám khảo.");

        _context.UserRoles.Add(new UserRoles { UserId = userId, RoleId = 4 });
        await _context.SaveChangesAsync();
        return (true, $"Đã thêm {user.FullName} vào danh sách giám khảo.");
    }

    public async Task<(bool success, string message)> RemoveUserFromJudgePoolAsync(int userId)
    {
        var ur = await _context.UserRoles
            .FirstOrDefaultAsync(x => x.UserId == userId && x.RoleId == 4);
        if (ur == null) return (false, "Người dùng không phải giám khảo.");

        bool hasRoundAssignments = await _context.Judges.AnyAsync(j => j.UserId == userId);
        if (hasRoundAssignments)
            return (false, "Giám khảo này vẫn đang được phân công vào các vòng thi. Hãy xóa khỏi vòng thi trước.");

        _context.UserRoles.Remove(ur);
        await _context.SaveChangesAsync();

        var user = await _context.Users.FindAsync(userId);
        return (true, $"Đã xóa {user?.FullName ?? "người dùng"} khỏi danh sách giám khảo.");
    }

    public async Task<List<UserSearchDto>> SearchUsersForJudgePoolAsync(string search)
    {
        var existingJudgeIds = await _context.UserRoles
            .Where(ur => ur.RoleId == 4)
            .Select(ur => ur.UserId)
            .ToListAsync();

        return await _context.Users
            .Where(u => u.IsActive
                && !existingJudgeIds.Contains(u.UserId)
                && (u.FullName.Contains(search) || u.Email.Contains(search)))
            .OrderBy(u => u.FullName)
            .Take(8)
            .Select(u => new UserSearchDto
            {
                UserId    = u.UserId,
                FullName  = u.FullName,
                Email     = u.Email,
                StudentId = u.StudentId
            })
            .ToListAsync();
    }

    public async Task<List<UserSearchDto>> SearchJudgesForRoundAsync(int competitionId, int roundId, string search)
    {
        var poolIds = await _context.UserRoles
            .Where(ur => ur.RoleId == 4)
            .Select(ur => ur.UserId)
            .ToListAsync();

        var alreadyInRound = await _context.Judges
            .Where(j => j.CompetitionId == competitionId && j.RoundId == roundId)
            .Select(j => j.UserId)
            .ToListAsync();

        return await _context.Users
            .Where(u => u.IsActive
                && poolIds.Contains(u.UserId)
                && !alreadyInRound.Contains(u.UserId)
                && (u.FullName.Contains(search) || u.Email.Contains(search)))
            .OrderBy(u => u.FullName)
            .Take(8)
            .Select(u => new UserSearchDto
            {
                UserId    = u.UserId,
                FullName  = u.FullName,
                Email     = u.Email,
                StudentId = u.StudentId
            })
            .ToListAsync();
    }

    public async Task<List<TimeConflictDto>> CheckTimeConflictsAsync(int judgeUserId, int roundId)
    {
        var newRound = await _context.CompetitionRounds.FindAsync(roundId);
        if (newRound == null) return new();

        var existing = await _context.Judges
            .Include(j => j.Round).ThenInclude(r => r!.Competition)
            .Where(j => j.UserId == judgeUserId && j.RoundId.HasValue && j.RoundId != roundId)
            .ToListAsync();

        return existing
            .Where(j => j.Round != null
                && j.Round.StartDate < newRound.EndDate
                && j.Round.EndDate   > newRound.StartDate)
            .Select(j => new TimeConflictDto
            {
                JudgeUserId             = judgeUserId,
                ExistingCompetitionName = j.Round!.Competition?.CompetitionName ?? "",
                ExistingRoundId         = j.Round.RoundId,
                ExistingRoundName       = j.Round.RoundName,
                ExistingStartDate       = j.Round.StartDate,
                ExistingEndDate         = j.Round.EndDate,
                ExistingRole            = j.JudgeRole
            })
            .ToList();
    }

    private static string RoleText(string role) => role switch
    {
        "HeadJudge" => "Trưởng ban",
        "ViceHead"  => "Phó ban",
        _           => "Thành viên"
    };
}
