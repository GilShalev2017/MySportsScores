using Common.Models;
using Confluent.Kafka;
using MongoDB.Driver;
using Nest;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace IngestService.Repositories
{
    public interface ISportEventRepository
    {
        Task SaveEventToMongoAsync(SportEvent sportEvent);
        Task SaveEventToElasticsearchAsync(SportEvent sportEvent);
        Task UpdateRedisScoreAsync(int matchId, int homeScore, int awayScore);
    }

    public class SportEventRepository : ISportEventRepository
    {
        private readonly IMongoDatabase _mongoDb;
        private readonly IElasticClient _elasticClient;
        private readonly IDatabase _redisDb;
        private readonly ILogger<SportEventRepository> _logger;

        public SportEventRepository(
            IMongoDatabase mongoDb,
            IElasticClient elasticClient,
            IConnectionMultiplexer redis,
            ILogger<SportEventRepository> logger)
        {
            _mongoDb = mongoDb;
            _elasticClient = elasticClient;
            _redisDb = redis.GetDatabase();
            _logger = logger;
        }

        public static object ConvertJsonElement(JsonElement je)
        {
            return je.ValueKind switch
            {
                JsonValueKind.Object => je.EnumerateObject()
                                          .ToDictionary(p => p.Name, p => ConvertJsonElement(p.Value)),
                JsonValueKind.Array => je.EnumerateArray().Select(ConvertJsonElement).ToList(),
                JsonValueKind.String => je.GetString()!,
                JsonValueKind.Number => je.TryGetInt64(out var l) ? (object)l : je.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null!,
                _ => je.GetRawText()
            };
        }

        public static Dictionary<string, object> SanitizeMetadata(Dictionary<string, object> metadata)
        {
            return metadata.ToDictionary(
                kv => kv.Key,
                kv => kv.Value is JsonElement je ? ConvertJsonElement(je) : kv.Value
            );
        }

        public async Task SaveEventToMongoAsync(SportEvent sportEvent)
        {
            try
            {
                var collection = _mongoDb.GetCollection<SportEvent>("match_events");

                sportEvent.Metadata = SanitizeMetadata(sportEvent.Metadata);

                await collection.InsertOneAsync(sportEvent);
                _logger.LogDebug("Saved event {EventId} to MongoDB", sportEvent.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving event to MongoDB");
                throw;
            }
        }

        public async Task SaveEventToElasticsearchAsync(SportEvent sportEvent)
        {
            try
            {
                var response = await _elasticClient.IndexDocumentAsync(sportEvent);
                if (!response.IsValid)
                {
                    _logger.LogError("Error indexing to Elasticsearch: {Error}", response.DebugInformation);
                }
                else
                {
                    _logger.LogDebug("Indexed event {EventId} to Elasticsearch", sportEvent.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving event to Elasticsearch");
                throw;
            }
        }

        public async Task UpdateRedisScoreAsync(int matchId, int homeScore, int awayScore)
        {
            try
            {
                var key = $"match:score:{matchId}";
                var scoreData = new
                {
                    matchId,
                    homeScore,
                    awayScore,
                    timestamp = DateTime.UtcNow
                };

                var json = JsonSerializer.Serialize(scoreData);
                await _redisDb.StringSetAsync(key, json, TimeSpan.FromMinutes(5));

                // Also add to sorted set of live matches
                await _redisDb.SortedSetAddAsync("live:matches", matchId, DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                _logger.LogDebug("Updated Redis score for match {MatchId}: {HomeScore}-{AwayScore}",
                    matchId, homeScore, awayScore);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Redis score");
                throw;
            }
        }
    }

}
