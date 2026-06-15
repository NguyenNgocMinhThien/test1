using Web_cham_diem.Models.ViewModels;

namespace Web_cham_diem.Services;

public interface IResultsService
{
    Task<ResultsPageViewModel> GetResultsViewAsync(int? competitionId);
}
