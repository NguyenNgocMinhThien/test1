using Web_cham_diem.Models.ViewModels;

namespace Web_cham_diem.Services;

public interface IGradingService
{
    Task<OrganizerGradingViewModel> GetGradingViewAsync(int? competitionId);
    Task<List<UserSearchDto>> SearchUsersForJudgeAsync(int competitionId, string search);
    Task<(bool success, string message)> AddJudgeAsync(int competitionId, AddJudgeDto dto);
    Task<(bool success, string message)> RemoveJudgeAsync(int judgeId, int competitionId);
    Task<(bool success, string message)> UpdateJudgeRoleAsync(int judgeId, int competitionId, UpdateJudgeRoleDto dto);
    Task<(bool success, string message)> AssignSubmissionsAsync(int competitionId, AssignSubmissionsDto dto, int assignedByUserId);
    Task<(bool success, string message)> RevokeAssignmentAsync(int assignmentId, int competitionId);
}
