using Web_cham_diem.Models;

namespace Web_cham_diem.Services;

public interface ICompetitionService
{
    Task<List<Competitions>> GetAllCompetitionsAsync();
    Task<Competitions> GetCompetitionByIdAsync(int id);
    Task<List<Teams>> GetCompetitionTeamsAsync(int competitionId);
    Task<List<Registrations>> GetCompetitionRegistrationsAsync(int competitionId);
}