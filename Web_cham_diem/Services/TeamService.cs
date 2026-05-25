using Web_cham_diem.Models;
using Microsoft.EntityFrameworkCore;

namespace Web_cham_diem.Services;

public class TeamService : ITeamService
{
    private readonly ApplicationDbContext _context;

    public TeamService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Competitions>> GetAllCompetitionsAsync()
    {
        return await _context.Competitions
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<Teams> GetTeamByIdAsync(int id)
    {
        return await _context.Teams
            .Include(t => t.Leader)
            .Include(t => t.Competition)
            .Include(t => t.Registrations)
                .ThenInclude(r => r.User)
            .FirstOrDefaultAsync(t => t.TeamId == id);
    }

    public async Task<List<Teams>> GetAllTeamsAsync()
    {
        return await _context.Teams
            .Include(t => t.Leader)
            .Include(t => t.Competition)
            .ToListAsync();
    }
}