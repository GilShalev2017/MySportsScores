using Common.Models;
using IngestService.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IngestService.Repositories
{
    public interface IPlayerRepository : IRepository<Player>
    {
        Task<IEnumerable<Player>> GetPlayersByTeamAsync(int teamId);
    }

    public class PlayerRepository : IPlayerRepository
    {
        private readonly SportsDbContext _context;

        public PlayerRepository(SportsDbContext context)
        {
            _context = context;
        }

        public async Task<Player> GetByIdAsync(int id)
        {
            return await _context.Players.FindAsync(id);
        }

        public async Task<IEnumerable<Player>> GetAllAsync()
        {
            return await _context.Players.ToListAsync();
        }

        public async Task<Player> AddAsync(Player entity)
        {
            _context.Players.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task UpdateAsync(Player entity)
        {
            _context.Players.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var player = await GetByIdAsync(id);
            if (player != null)
            {
                _context.Players.Remove(player);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Player>> GetPlayersByTeamAsync(int teamId)
        {
            return await _context.Players
                .Where(p => p.TeamId == teamId)
                .ToListAsync();
        }
    }
}
