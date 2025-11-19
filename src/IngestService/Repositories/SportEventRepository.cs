using System.Text.Json;
using MongoDB.Driver;
using Nest;
using StackExchange.Redis;
using Common.Models;

namespace IngestService.Repositories
{
    public interface ISportEventRepository
    {
        //MongoDB operations
        Task SaveEventToMongoAsync(SportEvent sportEvent);
        //Redis operations
        Task UpdateRedisScoreAsync(int matchId, int homeScore, int awayScore);
        //Elasticsearch operations
        Task SaveEventToElasticsearchAsync(SportEvent sportEvent);
        Task IndexMatchToElasticsearchAsync(Match match);
        Task IndexPlayerToElasticsearchAsync(Player player);
    }

    public class SportEventRepository : ISportEventRepository
    {
        private readonly IMongoDatabase _mongoDb;
        private readonly IElasticClient _elasticClient;
        private readonly IDatabase _redisDb;
        private readonly ILogger<SportEventRepository> _logger;

        // Elasticsearch index names
        private const string MatchesIndex = "matches-index";
        private const string PlayersIndex = "players-index";
        private const string EventsIndex = "sports-events-index";

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

            // Initialize Elasticsearch indexes on startup
            //InitializeElasticsearchIndexesAsync().Wait();
        }

        private async Task InitializeElasticsearchIndexesAsync()
        {
            try
            {
                // Create Matches Index
                var matchesExist = await _elasticClient.Indices.ExistsAsync(MatchesIndex);
                if (!matchesExist.Exists)
                {
                    var createMatchesResponse = await _elasticClient.Indices.CreateAsync(MatchesIndex, c => c
                        .Settings(s => s
                            .NumberOfShards(3)
                            .NumberOfReplicas(1)
                        )
                        .Map<Match>(m => m
                            .AutoMap()
                            .Properties(p => p
                                .Number(n => n.Name(x => x.Id))
                                .Number(n => n.Name(x => x.HomeTeamId))
                                .Number(n => n.Name(x => x.AwayTeamId))
                                .Number(n => n.Name(x => x.LeagueId))
                                .Number(n => n.Name(x => x.HomeScore))
                                .Number(n => n.Name(x => x.AwayScore))
                                .Number(n => n.Name(x => x.Minute))
                                .Keyword(k => k.Name(x => x.Status))
                                .Date(d => d.Name(x => x.ScheduledTime))
                                .Text(t => t.Name(x => x.Venue).Analyzer("standard"))
                            )
                        )
                    );

                    if (createMatchesResponse.IsValid)
                    {
                        _logger.LogInformation("✅ Created Elasticsearch index: {Index}", MatchesIndex);
                    }
                    else
                    {
                        _logger.LogError("❌ Failed to create {Index}: {Error}", MatchesIndex, createMatchesResponse.DebugInformation);
                    }
                }

                // Create Players Index
                var playersExist = await _elasticClient.Indices.ExistsAsync(PlayersIndex);
                if (!playersExist.Exists)
                {
                    var createPlayersResponse = await _elasticClient.Indices.CreateAsync(PlayersIndex, c => c
                        .Settings(s => s
                            .NumberOfShards(2)
                            .NumberOfReplicas(1)
                        )
                        .Map<Player>(m => m
                            .AutoMap()
                            .Properties(p => p
                                .Number(n => n.Name(x => x.Id))
                                .Text(t => t
                                    .Name(x => x.FirstName)
                                    .Analyzer("standard")
                                    .Fields(f => f
                                        .Keyword(k => k.Name("keyword"))
                                    )
                                )
                                .Text(t => t
                                    .Name(x => x.LastName)
                                    .Analyzer("standard")
                                    .Fields(f => f
                                        .Keyword(k => k.Name("keyword"))
                                    )
                                )
                                .Text(t => t
                                    .Name(x => x.FullName)
                                    .Analyzer("standard")
                                )
                                .Number(n => n.Name(x => x.TeamId))
                                .Keyword(k => k.Name(x => x.Position))
                                .Number(n => n.Name(x => x.ShirtNumber))
                                .Keyword(k => k.Name(x => x.Nationality))
                                .Date(d => d.Name(x => x.DateOfBirth))
                            )
                        )
                    );

                    if (createPlayersResponse.IsValid)
                    {
                        _logger.LogInformation("✅ Created Elasticsearch index: {Index}", PlayersIndex);
                    }
                    else
                    {
                        _logger.LogError("❌ Failed to create {Index}: {Error}", PlayersIndex, createPlayersResponse.DebugInformation);
                    }
                }

                // Create Events Index
                var eventsExist = await _elasticClient.Indices.ExistsAsync(EventsIndex);
                if (!eventsExist.Exists)
                {
                    var createEventsResponse = await _elasticClient.Indices.CreateAsync(EventsIndex, c => c
                        .Settings(s => s
                            .NumberOfShards(3)
                            .NumberOfReplicas(1)
                        )
                        .Map<SportEvent>(m => m
                            .AutoMap()
                            .Properties(p => p
                                .Keyword(k => k.Name(x => x.Id))
                                .Number(n => n.Name(x => x.MatchId))
                                .Keyword(k => k.Name(x => x.Type))
                                .Number(n => n.Name(x => x.Minute))
                                .Number(n => n.Name(x => x.PlayerId).NullValue(-1))
                                .Number(n => n.Name(x => x.TeamId).NullValue(-1))
                                .Text(t => t
                                    .Name(x => x.Description)
                                    .Analyzer("standard")
                                )
                                .Date(d => d.Name(x => x.Timestamp))
                                .Object<Dictionary<string, object>>(o => o
                                    .Name(x => x.Metadata)
                                    .Enabled(false)
                                )
                            )
                        )
                    );

                    if (createEventsResponse.IsValid)
                    {
                        _logger.LogInformation("✅ Created Elasticsearch index: {Index}", EventsIndex);
                    }
                    else
                    {
                        _logger.LogError("❌ Failed to create {Index}: {Error}", EventsIndex, createEventsResponse.DebugInformation);
                    }
                }

                _logger.LogInformation("✅ All Elasticsearch indexes initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error initializing Elasticsearch indexes");
            }
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
                var response = await _elasticClient.IndexAsync(sportEvent, idx => idx
                    .Index(EventsIndex)
                    .Id(sportEvent.Id)
                );

                if (!response.IsValid)
                {
                    _logger.LogError("Error indexing event to Elasticsearch: {Error}", response.DebugInformation);
                }
                else
                {
                    _logger.LogDebug("Indexed event {EventId} to Elasticsearch ({Index})", sportEvent.Id, EventsIndex);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving event to Elasticsearch");
                throw;
            }
        }

        public async Task IndexMatchToElasticsearchAsync(Match match)
        {
            try
            {
                var response = await _elasticClient.IndexAsync(match, idx => idx
                    .Index(MatchesIndex)
                    .Id(match.Id.ToString())
                );

                if (!response.IsValid)
                {
                    _logger.LogError("Error indexing match to Elasticsearch: {Error}", response.DebugInformation);
                }
                else
                {
                    _logger.LogDebug("Indexed match {MatchId} to Elasticsearch ({Index})", match.Id, MatchesIndex);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error indexing match to Elasticsearch");
                throw;
            }
        }

        public async Task IndexPlayerToElasticsearchAsync(Player player)
        {
            try
            {
                var response = await _elasticClient.IndexAsync(player, idx => idx
                    .Index(PlayersIndex)
                    .Id(player.Id.ToString())
                );

                if (!response.IsValid)
                {
                    _logger.LogError("Error indexing player to Elasticsearch: {Error}", response.DebugInformation);
                }
                else
                {
                    _logger.LogDebug("Indexed player {PlayerId} ({FullName}) to Elasticsearch ({Index})",
                        player.Id, player.FullName, PlayersIndex);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error indexing player to Elasticsearch");
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