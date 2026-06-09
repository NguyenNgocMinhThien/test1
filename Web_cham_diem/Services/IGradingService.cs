using Web_cham_diem.Models.ViewModels;

namespace Web_cham_diem.Services;

public interface IGradingService
{
    Task<OrganizerGradingViewModel> GetGradingViewAsync(int? competitionId);
    Task<List<UserSearchDto>> SearchUsersForJudgeAsync(int competitionId, string search, int? roundId = null);
    Task<List<UserSearchDto>> SearchUsersForJudgePoolAsync(string search);
    Task<List<UserSearchDto>> SearchJudgesForRoundAsync(int competitionId, int roundId, string search);
    Task<List<JudgePoolItemDto>> GetJudgePoolAsync();
    Task<(bool success, string message)> AddUserToJudgePoolAsync(int userId);
    Task<(bool success, string message)> RemoveUserFromJudgePoolAsync(int userId);
    Task<List<TimeConflictDto>> CheckTimeConflictsAsync(int judgeUserId, int roundId);
    Task<(bool success, string message)> AddJudgeAsync(int competitionId, AddJudgeDto dto);
    Task<(bool success, string message)> RemoveJudgeAsync(int judgeId, int competitionId);
    Task<(bool success, string message)> UpdateJudgeRoleAsync(int judgeId, int competitionId, UpdateJudgeRoleDto dto);
    Task<(bool success, string message)> AssignSubmissionsAsync(int competitionId, AssignSubmissionsDto dto, int assignedByUserId);
    Task<(bool success, string message)> RevokeAssignmentAsync(int assignmentId, int competitionId);
}
