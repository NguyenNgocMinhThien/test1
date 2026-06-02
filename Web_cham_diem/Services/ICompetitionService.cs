using Web_cham_diem.Models;
using Web_cham_diem.Models.ViewModels;

namespace Web_cham_diem.Services;

public interface ICompetitionService
{
    Task<List<Competitions>> GetAllCompetitionsAsync();
    Task<Competitions> GetCompetitionByIdAsync(int id);
    Task<List<Teams>> GetCompetitionTeamsAsync(int competitionId);
    Task<List<Registrations>> GetCompetitionRegistrationsAsync(int competitionId);
    Task<OrganizerContestsViewModel> GetOrganizerContestsAsync(string? searchQuery, string? statusFilter, string? categoryFilter, int pageNumber = 1);
    Task<CompetitionDetailViewModel> GetCompetitionDetailAsync(int competitionId);
    Task<int> CreateCompetitionAsync(CreateCompetitionViewModel model);
    Task<EditCompetitionViewModel> GetCompetitionForEditAsync(int competitionId);
    Task<bool> UpdateCompetitionAsync(int competitionId, EditCompetitionViewModel model);
    Task<bool> DeleteCompetitionAsync(int competitionId);
    Task<bool> ChangeCompetitionStatusAsync(int competitionId, string newStatus);
    Task<OrganizerDashboardViewModel> GetOrganizerDashboardDataAsync();
}