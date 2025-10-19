using Common.DTOs;
using Common.Repositories;
using MongoDB.Driver;
using StackExchange.Redis;
using System.Text.Json;

namespace DataAPI.Services
{
    public interface IMatchService
    {
        Task<IEnumerable<LiveMatchDto>> GetLiveMatchesAsync();
        Task<LiveMatchDto> GetMatchByIdAsync(int matchId);
        Task<IEnumerable<LiveMatchDto>> GetMatchesByLeagueAsync(int leagueId);
        Task<IEnumerable<LiveMatchDto>> GetMatchesByTeamAsync(int teamId);
        Task<object> GetMatchEventsAsync(int matchId);
    }

    public class MatchService : IMatchService
    {
        private readonly IMatchRepository _matchRepository;
        private readonly IDatabase _redisDb;
        private readonly IMongoDatabase _mongoDb;
        private readonly ILogger<MatchService> _logger;

        public MatchService(
            IMatchRepository matchRepository,
            IConnectionMultiplexer redis,
            IMongoDatabase mongoDb,
            ILogger<MatchService> logger)
        {
            _matchRepository = matchRepository;
            _redisDb = redis.GetDatabase();
            _mongoDb = mongoDb;
            _logger = logger;
        }

        public async Task<IEnumerable<LiveMatchDto>> GetLiveMatchesAsync()
        {
            try
            {
                // Try Redis cache first
                var cachedMatches = await _redisDb.StringGetAsync("live:matches:all");
                if (!cachedMatches.IsNullOrEmpty)
                {
                    _logger.LogDebug("Returning live matches from Redis cache");
                    return JsonSerializer.Deserialize<IEnumerable<LiveMatchDto>>(cachedMatches);
                }

                // Fetch from SQL Server
                var matches = await _matchRepository.GetLiveMatchesAsync();
                var dtos = matches.Select(m => new LiveMatchDto
                {
                    MatchId = m.Id,
                    HomeTeam = $"Team {m.HomeTeamId}",
                    AwayTeam = $"Team {m.AwayTeamId}",
                    HomeScore = m.HomeScore,
                    AwayScore = m.AwayScore,
                    Minute = m.Minute,
                    Status = m.Status.ToString(),
                    League = $"League {m.LeagueId}"
                }).ToList();

                // Cache for 5 seconds
                var json = JsonSerializer.Serialize(dtos);
                await _redisDb.StringSetAsync("live:matches:all", json, TimeSpan.FromSeconds(5));

                return dtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting live matches");
                throw;
            }
        }

        public async Task<LiveMatchDto> GetMatchByIdAsync(int matchId)
        {
            try
            {
                // Try Redis cache first
                var cacheKey = $"match:score:{matchId}";
                var cachedScore = await _redisDb.StringGetAsync(cacheKey);

                if (!cachedScore.IsNullOrEmpty)
                {
                    _logger.LogDebug("Returning match {MatchId} from Redis cache", matchId);
                    var scoreData = JsonSerializer.Deserialize<Dictionary<string, object>>(cachedScore);
                    // Transform to LiveMatchDto
                }

                // Fetch from SQL Server
                var match = await _matchRepository.GetByIdAsync(matchId);
                if (match == null)
                    return null;

                return new LiveMatchDto
                {
                    MatchId = match.Id,
                    HomeTeam = $"Team {match.HomeTeamId}",
                    AwayTeam = $"Team {match.AwayTeamId}",
                    HomeScore = match.HomeScore,
                    AwayScore = match.AwayScore,
                    Minute = match.Minute,
                    Status = match.Status.ToString(),
                    League = $"League {match.LeagueId}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting match {MatchId}", matchId);
                throw;
            }
        }

        public async Task<IEnumerable<LiveMatchDto>> GetMatchesByLeagueAsync(int leagueId)
        {
            var matches = await _matchRepository.GetMatchesByLeagueAsync(leagueId);
            return matches.Select(m => new LiveMatchDto
            {
                MatchId = m.Id,
                HomeTeam = $"Team {m.HomeTeamId}",
                AwayTeam = $"Team {m.AwayTeamId}",
                HomeScore = m.HomeScore,
                AwayScore = m.AwayScore,
                Minute = m.Minute,
                Status = m.Status.ToString(),
                League = $"League {m.LeagueId}"
            });
        }

        public async Task<IEnumerable<LiveMatchDto>> GetMatchesByTeamAsync(int teamId)
        {
            var allMatches = await _matchRepository.GetAllAsync();
            var matches = allMatches.Where(m => m.HomeTeamId == teamId || m.AwayTeamId == teamId);

            return matches.Select(m => new LiveMatchDto
            {
                MatchId = m.Id,
                HomeTeam = $"Team {m.HomeTeamId}",
                AwayTeam = $"Team {m.AwayTeamId}",
                HomeScore = m.HomeScore,
                AwayScore = m.AwayScore,
                Minute = m.Minute,
                Status = m.Status.ToString(),
                League = $"League {m.LeagueId}"
            });
        }

        public async Task<object> GetMatchEventsAsync(int matchId)
        {
            try
            {
                // Fetch from MongoDB
                var collection = _mongoDb.GetCollection<Common.Models.SportEvent>("match_events");
                var filter = Builders<Common.Models.SportEvent>.Filter.Eq(e => e.MatchId, matchId);
                var events = await collection.Find(filter)
                    .SortBy(e => e.Minute)
                    .ToListAsync();

                return events;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting events for match {MatchId}", matchId);
                throw;
            }
        }
    }
}