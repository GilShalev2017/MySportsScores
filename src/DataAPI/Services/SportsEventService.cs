using Common.Models;
using MongoDB.Driver;

namespace DataAPI.Services
{
    public interface ISportEventService
    {
        Task<IEnumerable<SportEvent>> GetMatchEventsAsync(int matchId);
        Task<IEnumerable<SportEvent>> GetRecentEventsAsync(int limit = 100);
        Task<IEnumerable<SportEvent>> GetEventsByTypeAsync(EventType type, int limit = 50);
    }

    public class SportEventService : ISportEventService
    {
        private readonly IMongoCollection<SportEvent> _eventsCollection;
        private readonly ILogger<SportEventService> _logger;

        public SportEventService(IMongoDatabase mongoDatabase, ILogger<SportEventService> logger)
        {
            _eventsCollection = mongoDatabase.GetCollection<SportEvent>("match_events");
            _logger = logger;
        }

        public async Task<IEnumerable<SportEvent>> GetMatchEventsAsync(int matchId)
        {
            try
            {
                var filter = Builders<SportEvent>.Filter.Eq(e => e.MatchId, matchId);
                return await _eventsCollection
                    .Find(filter)
                    .SortBy(e => e.Minute)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching events for match {MatchId}", matchId);
                throw;
            }
        }

        public async Task<IEnumerable<SportEvent>> GetRecentEventsAsync(int limit = 100)
        {
            try
            {
                return await _eventsCollection
                    .Find(_ => true)
                    .SortByDescending(e => e.Timestamp)
                    .Limit(limit)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching recent events");
                throw;
            }
        }

        public async Task<IEnumerable<SportEvent>> GetEventsByTypeAsync(EventType type, int limit = 50)
        {
            try
            {
                var filter = Builders<SportEvent>.Filter.Eq(e => e.Type, type);
                return await _eventsCollection
                    .Find(filter)
                    .SortByDescending(e => e.Timestamp)
                    .Limit(limit)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching events of type {EventType}", type);
                throw;
            }
        }
    }
}
