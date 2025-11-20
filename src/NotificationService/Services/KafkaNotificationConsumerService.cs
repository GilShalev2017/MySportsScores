using Common.Events;
using Confluent.Kafka;
using Microsoft.AspNetCore.SignalR;
using NotificationService.Hubs;
using System.Text.Json;

namespace NotificationService.Services
{
    public class KafkaNotificationConsumerService : BackgroundService
    {
        private readonly IHubContext<SportsNotificationHub> _hubContext;
        private readonly IUserPreferenceService _userPreferenceService;
        private readonly ILogger<KafkaNotificationConsumerService> _logger;
        private readonly IConfiguration _configuration;
        private long _totalNotificationsSent = 0;

        public KafkaNotificationConsumerService(
            IHubContext<SportsNotificationHub> hubContext,
            IUserPreferenceService userPreferenceService,
            ILogger<KafkaNotificationConsumerService> logger,
            IConfiguration configuration)
        {
            _hubContext = hubContext;
            _userPreferenceService = userPreferenceService;
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("KafkaNotificationConsumerService started");

            var config = new ConsumerConfig
            {
                BootstrapServers = _configuration["Kafka:BootstrapServers"],
                GroupId = "notification-service-group",
                AutoOffsetReset = AutoOffsetReset.Latest,
                EnableAutoCommit = true
            };

            // Start consuming from multiple topics
            var consumerTasks = new List<Task>
            {
                ConsumeLiveScoresAsync(config, stoppingToken),
                ConsumeIngestEventsAsync(config, stoppingToken),
                ConsumePlayerUpdatesAsync(config, stoppingToken)
            };

            // Start metrics reporting
            _ = Task.Run(() => ReportMetrics(stoppingToken), stoppingToken);

            await Task.WhenAll(consumerTasks);
        }

        private async Task ConsumeLiveScoresAsync(ConsumerConfig config, CancellationToken stoppingToken)
        {
            var consumerConfig = new ConsumerConfig(config)
            {
                GroupId = $"{config.GroupId}-live-scores"
            };

            using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
            consumer.Subscribe("live-scores");

            _logger.LogInformation("Subscribed to live-scores topic");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = consumer.Consume(TimeSpan.FromSeconds(1));

                    if (consumeResult != null)
                    {
                        var scoreUpdate = JsonSerializer.Deserialize<ScoreUpdate>(
                            consumeResult.Message.Value,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (scoreUpdate != null)
                        {
                            await BroadcastScoreUpdateAsync(scoreUpdate);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error consuming live-scores");
                }
            }
        }

        private async Task ConsumeIngestEventsAsync(ConsumerConfig config, CancellationToken stoppingToken)
        {
            var consumerConfig = new ConsumerConfig(config)
            {
                GroupId = $"{config.GroupId}-ingest-events"
            };

            using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
            consumer.Subscribe("ingest-events");

            _logger.LogInformation("Subscribed to ingest-events topic");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = consumer.Consume(TimeSpan.FromSeconds(1));

                    if (consumeResult != null)
                    {
                        var sportEvent = JsonSerializer.Deserialize<SportEventUpdate>(
                            consumeResult.Message.Value,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (sportEvent != null)
                        {
                            await BroadcastSportEventAsync(sportEvent);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error consuming ingest-events");
                }
            }
        }

        private async Task ConsumePlayerUpdatesAsync(ConsumerConfig config, CancellationToken stoppingToken)
        {
            var consumerConfig = new ConsumerConfig(config)
            {
                GroupId = $"{config.GroupId}-player-updates"
            };

            using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
            consumer.Subscribe("player-updates");

            _logger.LogInformation("Subscribed to player-updates topic");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = consumer.Consume(TimeSpan.FromSeconds(1));

                    if (consumeResult != null)
                    {
                        var playerUpdate = JsonSerializer.Deserialize<PlayerUpdate>(
                            consumeResult.Message.Value,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (playerUpdate != null)
                        {
                            await BroadcastPlayerUpdateAsync(playerUpdate);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error consuming player-updates");
                }
            }
        }

        private async Task BroadcastPlayerUpdateAsync(PlayerUpdate playerUpdate)
        {
            try
            {
                // Broadcast to users following this player
                await _hubContext.Clients.Group($"player_{playerUpdate.PlayerId}")
                    .SendAsync("PlayerUpdate", playerUpdate);

                // Also broadcast to match watchers (player stats in the match)
                await _hubContext.Clients.Group($"match_{playerUpdate.MatchId}")
                    .SendAsync("PlayerUpdate", playerUpdate);

                Interlocked.Increment(ref _totalNotificationsSent);

                _logger.LogDebug("Broadcasted player update for player {PlayerId} in match {MatchId}",
                    playerUpdate.PlayerId, playerUpdate.MatchId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting player update");
            }
        }
       
        private async Task BroadcastScoreUpdateAsync(ScoreUpdate scoreUpdate)
        {
            try
            {
                // Broadcast to all users watching this match
                await _hubContext.Clients.Group($"match_{scoreUpdate.MatchId}")
                    .SendAsync("ScoreUpdate", scoreUpdate);

                Interlocked.Increment(ref _totalNotificationsSent);

                _logger.LogDebug("Broadcasted score update for match {MatchId}: {HomeScore}-{AwayScore}",
                    scoreUpdate.MatchId, scoreUpdate.HomeScore, scoreUpdate.AwayScore);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting score update");
            }
        }

        private async Task BroadcastSportEventAsync(SportEventUpdate sportEvent)
        {
            try
            {
                // Broadcast to match watchers
                await _hubContext.Clients.Group($"match_{sportEvent.MatchId}")
                    .SendAsync("SportEvent", sportEvent);

                // Broadcast to team followers if team is involved
                if (sportEvent.TeamId.HasValue)
                {
                    await _hubContext.Clients.Group($"team_{sportEvent.TeamId.Value}")
                        .SendAsync("TeamEvent", sportEvent);
                }

                // Broadcast to player followers if player is involved
                if (sportEvent.PlayerId.HasValue)
                {
                    await _hubContext.Clients.Group($"player_{sportEvent.PlayerId.Value}")
                        .SendAsync("PlayerEvent", sportEvent);
                }

                Interlocked.Increment(ref _totalNotificationsSent);

                if (sportEvent.EventType == Common.Models.EventType.Goal)
                {
                    _logger.LogInformation("⚽ Broadcasted GOAL event for match {MatchId} to all subscribers",
                        sportEvent.MatchId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting sport event");
            }
        }

        private async Task ReportMetrics(CancellationToken stoppingToken)
        {
            var lastCount = 0L;

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(10000, stoppingToken);

                var currentCount = Interlocked.Read(ref _totalNotificationsSent);
                var notificationsPerSecond = (currentCount - lastCount) / 10.0;
                lastCount = currentCount;

                _logger.LogInformation(
                    "📊 NotificationService Metrics - Total Sent: {TotalNotifications:N0} | Notifications/sec: {NotificationsPerSec:N1}",
                    currentCount,
                    notificationsPerSecond);
            }
        }
    }

}
