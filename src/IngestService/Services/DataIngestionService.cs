using Common.Events;
using Common.Models;
using IngestService.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        private readonly IMatchRepository _matchRepository;
        private readonly IPlayerRepository _playerRepository;
        private readonly ISportEventRepository _sportEventRepository;
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

        public async Task ProcessSportEventAsync(SportEventUpdate eventUpdate)
        {
            try
            {
                var sportEvent = new SportEvent
                {
                    Id = eventUpdate.EventId,
                    MatchId = eventUpdate.MatchId,
                    Type = eventUpdate.EventType,
                    Minute = eventUpdate.Minute,
                    PlayerId = eventUpdate.PlayerId,
                    TeamId = eventUpdate.TeamId,
                    Description = eventUpdate.Description,
                    Metadata = eventUpdate.Metadata,
                    Timestamp = eventUpdate.Timestamp
                };

                // Save to MongoDB (for document storage)
                await _sportEventRepository.SaveEventToMongoAsync(sportEvent);

                // Index to Elasticsearch (for search)
                await _sportEventRepository.SaveEventToElasticsearchAsync(sportEvent);

                _logger.LogDebug("Processed sport event: {EventType} for match {MatchId}",
                    eventUpdate.EventType, eventUpdate.MatchId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing sport event");
                throw;
            }
        }

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

                    await _matchRepository.UpdateAsync(match);
                }

                // Update Redis cache
                await _sportEventRepository.UpdateRedisScoreAsync(
                    scoreUpdate.MatchId,
                    scoreUpdate.HomeScore,
                    scoreUpdate.AwayScore);

                _logger.LogInformation("Updated score for match {MatchId}: {HomeScore}-{AwayScore}",
                    scoreUpdate.MatchId, scoreUpdate.HomeScore, scoreUpdate.AwayScore);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing score update");
                throw;
            }
        }

        public async Task ProcessPlayerUpdateAsync(PlayerUpdate playerUpdate)
        {
            try
            {
                // Save player statistics to MongoDB
                var collection = _sportEventRepository as dynamic;
                // This would typically save to a player_stats collection

                _logger.LogDebug("Processed player update for player {PlayerId}", playerUpdate.PlayerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing player update");
                throw;
            }
        }
    }
}
