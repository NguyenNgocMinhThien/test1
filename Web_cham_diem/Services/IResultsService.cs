using Web_cham_diem.Models.ViewModels;

namespace Web_cham_diem.Services;

public interface IResultsService
{
    Task<ResultsPageViewModel> GetResultsViewAsync(int organizerId, int? competitionId, bool isAdmin = false);
    Task<(bool ok, string msg)> SetRoundResultsPublishedAsync(int roundId, int competitionId, bool publish, int performedByUserId);
    Task<OrganizerStatisticsViewModel> GetOrganizerStatisticsAsync(int organizerId, int? competitionId = null, bool isAdmin = false);
    Task<List<ResultHistoryItemDto>> GetResultHistoryAsync(int organizerId, int? competitionId, bool isAdmin = false);
}
