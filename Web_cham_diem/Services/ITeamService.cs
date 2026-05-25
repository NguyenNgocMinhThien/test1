using Web_cham_diem.Models;

namespace Web_cham_diem.Services;

public interface ITeamService
{
    Task<List<Competitions>> GetAllCompetitionsAsync();
    Task<Teams> GetTeamByIdAsync(int id);
    Task<List<Teams>> GetAllTeamsAsync();
}