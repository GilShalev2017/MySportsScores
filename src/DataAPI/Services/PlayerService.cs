using Common.DTOs;
using Common.Repositories;

namespace DataAPI.Services
{
    public interface IPlayerService
    {
        Task<object> GetPlayerByIdAsync(int playerId);
        Task<PlayerStatsDto> GetPlayerStatsAsync(int playerId);
        Task<IEnumerable<object>> GetPlayersByTeamAsync(int teamId);
    }

    public class PlayerService : IPlayerService
    {
        private readonly IPlayerRepository _playerRepository;
        private readonly ILogger<PlayerService> _logger;

        public PlayerService(IPlayerRepository playerRepository, ILogger<PlayerService> logger)
        {
            _playerRepository = playerRepository;
            _logger = logger;
        }

        public async Task<object> GetPlayerByIdAsync(int playerId)
        {
            var player = await _playerRepository.GetByIdAsync(playerId);
            return player;
        }

        public async Task<PlayerStatsDto> GetPlayerStatsAsync(int playerId)
        {
            var player = await _playerRepository.GetByIdAsync(playerId);
            if (player == null)
                return null;

            // In a real implementation, you'd aggregate stats from MongoDB
            return new PlayerStatsDto
            {
                PlayerId = player.Id,
                PlayerName = player.FullName,
                Team = $"Team {player.TeamId}",
                Goals = 0,
                Assists = 0,
                YellowCards = 0,
                RedCards = 0,
                MinutesPlayed = 0
            };
        }

        public async Task<IEnumerable<object>> GetPlayersByTeamAsync(int teamId)
        {
            var players = await _playerRepository.GetPlayersByTeamAsync(teamId);
            return players;
        }
    }
}
