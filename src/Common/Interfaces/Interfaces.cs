using Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Interfaces
{
    public interface IRepository<T> where T : class
    {
        Task<T> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<T> AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(int id);
    }
    
    //public interface IMatchRepository : IRepository<Match>
    //{
    //    Task<IEnumerable<Match>> GetLiveMatchesAsync();
    //    Task<IEnumerable<Match>> GetMatchesByLeagueAsync(int leagueId);
    //    Task<IEnumerable<Match>> GetMatchesByTeamAsync(int teamId);
    //}

    //public interface IPlayerRepository : IRepository<Player>
    //{
    //    Task<IEnumerable<Player>> GetPlayersByTeamAsync(int teamId);
    //}
}
