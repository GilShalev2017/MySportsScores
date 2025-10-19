using Common.Data;
using Common.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Repositories
{
    public interface IMatchRepository
    {
        Task<IEnumerable<Match>> GetLiveMatchesAsync();
        Task<IEnumerable<Match>> GetMatchesByLeagueAsync(int leagueId);
        Task UpsertAsync(Match match);
        Task<Match?> GetByIdAsync(int id);
        Task UpdateAsync(Match entity);
        Task<IEnumerable<Match>> GetAllAsync();
    }

    public class MatchRepository : IMatchRepository
    {
        private readonly SportsDbContext _context;

        public MatchRepository(SportsDbContext context)
        {
            _context = context;
        }

        public async Task<Match?> GetByIdAsync(int id)
        {
            return await _context.Matches.FindAsync(id);
        }

        public async Task<IEnumerable<Match>> GetAllAsync()
        {
            return await _context.Matches.ToListAsync();
        }

        public async Task<Match> AddAsync(Match entity)
        {
            _context.Matches.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task UpdateAsync(Match entity)
        {
            _context.Matches.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var match = await GetByIdAsync(id);
            if (match != null)
            {
                _context.Matches.Remove(match);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Match>> GetLiveMatchesAsync()
        {
            return await _context.Matches
                .Where(m => m.Status == MatchStatus.Live)
                .OrderBy(m => m.ScheduledTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<Match>> GetMatchesByLeagueAsync(int leagueId)
        {
            return await _context.Matches
                .Where(m => m.LeagueId == leagueId)
                .OrderByDescending(m => m.ScheduledTime)
                .ToListAsync();
        }

        public async Task UpsertAsync(Match match)
        {
            var existing = await _context.Matches.FindAsync(match.Id);
            if (existing == null)
            {
                _context.Matches.Add(match);
            }
            else
            {
                _context.Entry(existing).CurrentValues.SetValues(match);
            }
            await _context.SaveChangesAsync();
        }
    }
}
