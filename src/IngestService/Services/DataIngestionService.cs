//using Common.Events;
//using Common.Models;
//using Common.Repositories;
//using IngestService.Repositories;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace IngestService.Services
//{
//    public interface IDataIngestionService
//    {
//        Task ProcessSportEventAsync(SportEventUpdate eventUpdate);
//        Task ProcessScoreUpdateAsync(ScoreUpdate scoreUpdate);
//        Task ProcessPlayerUpdateAsync(PlayerUpdate playerUpdate);
//    }

//    public class DataIngestionService : IDataIngestionService
//    {
//        private readonly IMatchRepository _matchRepository;
//        private readonly IPlayerRepository _playerRepository;
//        private readonly ISportEventRepository _sportEventRepository;
//        private readonly ILogger<DataIngestionService> _logger;

//        public DataIngestionService(
//            IMatchRepository matchRepository,
//            IPlayerRepository playerRepository,
//            ISportEventRepository sportEventRepository,
//            ILogger<DataIngestionService> logger)
//        {
//            _matchRepository = matchRepository;
//            _playerRepository = playerRepository;
//            _sportEventRepository = sportEventRepository;
//            _logger = logger;
//        }

//        public async Task ProcessSportEventAsync(SportEventUpdate eventUpdate)
//        {
//            try
//            {
//                var sportEvent = new SportEvent
//                {
//                    Id = eventUpdate.EventId,
//                    MatchId = eventUpdate.MatchId,
//                    Type = eventUpdate.EventType,
//                    Minute = eventUpdate.Minute,
//                    PlayerId = eventUpdate.PlayerId,
//                    TeamId = eventUpdate.TeamId,
//                    Description = eventUpdate.Description,
//                    Metadata = eventUpdate.Metadata,
//                    Timestamp = eventUpdate.Timestamp
//                };

//                // Save to MongoDB (for document storage)
//                await _sportEventRepository.SaveEventToMongoAsync(sportEvent);

//                // Index to Elasticsearch (for search)
//                await _sportEventRepository.SaveEventToElasticsearchAsync(sportEvent);

//                _logger.LogDebug("Processed sport event: {EventType} for match {MatchId}",
//                    eventUpdate.EventType, eventUpdate.MatchId);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error processing sport event");
//                throw;
//            }
//        }

//        public async Task ProcessScoreUpdateAsync(ScoreUpdate scoreUpdate)
//        {
//            try
//            {
//                // Update SQL Server
//                var match = await _matchRepository.GetByIdAsync(scoreUpdate.MatchId);
//                if (match != null)
//                {
//                    match.HomeScore = scoreUpdate.HomeScore;
//                    match.AwayScore = scoreUpdate.AwayScore;
//                    match.Minute = scoreUpdate.Minute;
//                    match.Status = scoreUpdate.Status;
//                    match.UpdatedAt = DateTime.UtcNow;

//                    await _matchRepository.UpdateAsync(match);
//                }

//                // Update Redis cache
//                await _sportEventRepository.UpdateRedisScoreAsync(
//                    scoreUpdate.MatchId,
//                    scoreUpdate.HomeScore,
//                    scoreUpdate.AwayScore);

//                _logger.LogInformation("Updated score for match {MatchId}: {HomeScore}-{AwayScore}",
//                    scoreUpdate.MatchId, scoreUpdate.HomeScore, scoreUpdate.AwayScore);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error processing score update");
//                throw;
//            }
//        }

//        public async Task ProcessPlayerUpdateAsync(PlayerUpdate playerUpdate)
//        {
//            try
//            {
//                // Save player statistics to MongoDB
//                var collection = _sportEventRepository as dynamic;
//                // This would typically save to a player_stats collection

//                _logger.LogDebug("Processed player update for player {PlayerId}", playerUpdate.PlayerId);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error processing player update");
//                throw;
//            }
//        }
//    }
//}

using Common.Events;
using Common.Models;
using Common.Repositories;
using IngestService.Repositories;

namespace IngestService.Services
{
    public interface IDataIngestionService
    {
        Task ProcessSportEventAsync(SportEventUpdate eventUpdate);
        Task ProcessScoreUpdateAsync(ScoreUpdate scoreUpdate);
        Task ProcessPlayerUpdateAsync(PlayerUpdate playerUpdate);
    }

    public class DataIngestionService : IDataIngestionService
    {
        private readonly IMatchRepository _matchRepository;//SQL DB
        private readonly IPlayerRepository _playerRepository;//SQL DB
        private readonly ISportEventRepository _sportEventRepository;//MongoDB, Elasticsearch, Redis
        private readonly ILogger<DataIngestionService> _logger;

        public DataIngestionService(
            IMatchRepository matchRepository,
            IPlayerRepository playerRepository,
            ISportEventRepository sportEventRepository,
            ILogger<DataIngestionService> logger)
        {
            _matchRepository = matchRepository;
            _playerRepository = playerRepository;
            _sportEventRepository = sportEventRepository;
            _logger = logger;
        }
        //Save sport events to MongoDB and Elasticsearch
        public async Task ProcessSportEventAsync(SportEventUpdate eventUpdate)
        {
            try
            {
                var sportEvent = new SportEvent
                {
                    Id = eventUpdate.EventId.ToString(),
                    MatchId = eventUpdate.MatchId,
                    Type = eventUpdate.EventType,
                    Minute = eventUpdate.Minute,
                    PlayerId = eventUpdate.PlayerId,
                    TeamId = eventUpdate.TeamId,
                    Description = eventUpdate.Description,
                    Metadata = eventUpdate.Metadata,
                    Timestamp = eventUpdate.Timestamp
                };

                // Save to MongoDB (for document/event storage)
                await _sportEventRepository.SaveEventToMongoAsync(sportEvent);

                // Index to Elasticsearch (for search) - EVENTS INDEX
                await _sportEventRepository.SaveEventToElasticsearchAsync(sportEvent);

                _logger.LogDebug("Processed sport event: {EventType} for match {MatchId} at minute {Minute}",
                    eventUpdate.EventType, eventUpdate.MatchId, eventUpdate.Minute);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing sport event {EventId}", eventUpdate.EventId);
                throw;
            }
        }
        //Save score updates to SQL Server, MongoDB, Elasticsearch, and Redis
        public async Task ProcessScoreUpdateAsync(ScoreUpdate scoreUpdate)
        {
            try
            {
                // Update SQL Server
                var match = await _matchRepository.GetByIdAsync(scoreUpdate.MatchId);
                if (match != null)
                {
                    match.HomeScore = scoreUpdate.HomeScore;
                    match.AwayScore = scoreUpdate.AwayScore;
                    match.Minute = scoreUpdate.Minute;
                    match.Status = scoreUpdate.Status;
                    match.UpdatedAt = DateTime.UtcNow;

                    // Save to SQL
                    await _matchRepository.UpdateAsync(match);

                    // Index to Elasticsearch - MATCHES INDEX (for searching matches)
                    await _sportEventRepository.IndexMatchToElasticsearchAsync(match);

                    _logger.LogInformation("✅ Updated match {MatchId} in SQL and Elasticsearch: {HomeScore}-{AwayScore} ({Status}, {Minute}')",
                        scoreUpdate.MatchId, scoreUpdate.HomeScore, scoreUpdate.AwayScore, scoreUpdate.Status, scoreUpdate.Minute);
                }
                else
                {
                    _logger.LogWarning("⚠️ Match {MatchId} not found in database", scoreUpdate.MatchId);
                }

                // Update Redis cache (fast real-time access)
                await _sportEventRepository.UpdateRedisScoreAsync(
                    scoreUpdate.MatchId,
                    scoreUpdate.HomeScore,
                    scoreUpdate.AwayScore);

                // Create a sport event for this score update
                var sportEvent = new SportEvent
                {
                    Id = Guid.NewGuid().ToString(),
                    MatchId = scoreUpdate.MatchId,
                    Type = EventType.Goal,
                    Minute = scoreUpdate.Minute,
                    Description = $"Score updated: {scoreUpdate.HomeScore}-{scoreUpdate.AwayScore}",
                    Metadata = new Dictionary<string, object>
                    {
                        { "homeScore", scoreUpdate.HomeScore },
                        { "awayScore", scoreUpdate.AwayScore },
                        { "status", scoreUpdate.Status.ToString() }
                    },
                    Timestamp = DateTime.UtcNow
                };

                // Save score update event to MongoDB
                await _sportEventRepository.SaveEventToMongoAsync(sportEvent);

                // Index score update event to Elasticsearch
                await _sportEventRepository.SaveEventToElasticsearchAsync(sportEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing score update for match {MatchId}", scoreUpdate.MatchId);
                throw;
            }
        }
        //Save player updates to SQL Server, MongoDB, and Elasticsearch
        public async Task ProcessPlayerUpdateAsync(PlayerUpdate playerUpdate)
        {
            try
            {
                // Get player from repository
                var player = await _playerRepository.GetByIdAsync(playerUpdate.PlayerId);

                if (player != null)
                {
                    // Update player timestamp
                    player.UpdatedAt = DateTime.UtcNow;

                    // Note: If your IPlayerRepository doesn't have UpdateAsync, 
                    // you might need to add it to the interface and implementation
                    // For now, we'll just index to Elasticsearch

                    // Index to Elasticsearch - PLAYERS INDEX (for searching players)
                    await _sportEventRepository.IndexPlayerToElasticsearchAsync(player);

                    _logger.LogDebug("✅ Indexed player {PlayerId} ({FullName}) to Elasticsearch",
                        player.Id, player.FullName);
                }
                else
                {
                    _logger.LogWarning("⚠️ Player {PlayerId} not found in database", playerUpdate.PlayerId);
                }

                // Create a sport event for this player update
                var sportEvent = new SportEvent
                {
                    Id = Guid.NewGuid().ToString(),
                    MatchId = playerUpdate.MatchId,
                    Type = EventType.Substitution, // Or another appropriate type
                    Minute = 0, // Set actual minute if available in playerUpdate
                    PlayerId = playerUpdate.PlayerId,
                    Description = player != null
                        ? $"Player update: {player.FullName}"
                        : $"Player update: Player {playerUpdate.PlayerId}",
                    Metadata = new Dictionary<string, object>
                    {
                        { "playerId", playerUpdate.PlayerId },
                        { "updateType", "playerStats" }
                    },
                    Timestamp = DateTime.UtcNow
                };

                // Save player update event to MongoDB
                await _sportEventRepository.SaveEventToMongoAsync(sportEvent);

                // Index player update event to Elasticsearch
                await _sportEventRepository.SaveEventToElasticsearchAsync(sportEvent);

                _logger.LogDebug("Processed player update for player {PlayerId} in match {MatchId}",
                    playerUpdate.PlayerId, playerUpdate.MatchId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing player update for player {PlayerId}", playerUpdate.PlayerId);
                throw;
            }
        }
    }
}