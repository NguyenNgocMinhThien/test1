using Web_cham_diem.Models;
using Microsoft.EntityFrameworkCore;

namespace Web_cham_diem.Services;

public class CompetitionService : ICompetitionService
{
    private readonly ApplicationDbContext _context;

    public CompetitionService(ApplicationDbContext context)
    {
        _context = context;
    }

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
            .Include(c => c.ScoringCriteria)
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
}