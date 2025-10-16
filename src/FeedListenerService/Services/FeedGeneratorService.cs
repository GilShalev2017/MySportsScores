using Common.Events;
using Common.Models;
using FeedListenerService.Models;

namespace FeedListenerService.Services
{


    // =====================================================
    // Services/FeedGeneratorService.cs
    // =====================================================
    public class FeedGeneratorService : BackgroundService
    {
        private readonly IKafkaProducerService _kafka;
        private readonly ILogger<FeedGeneratorService> _logger;
        private readonly Random _random = new();
        private readonly List<MatchSimulation> _activeMatches = new();
        private long _totalEventsGenerated = 0;

        public FeedGeneratorService(IKafkaProducerService kafka, ILogger<FeedGeneratorService> logger)
        {
            _kafka = kafka;
            _logger = logger;
            InitializeMatches();
        }

        private void InitializeMatches()
        {
            // Create 20 simulated live matches
            for (int i = 1; i <= 20; i++)
            {
                _activeMatches.Add(new MatchSimulation
                {
                    MatchId = i,
                    HomeTeamId = i * 2 - 1,
                    AwayTeamId = i * 2,
                    LeagueId = (i % 5) + 1,
                    CurrentMinute = _random.Next(1, 45),
                    HomeScore = _random.Next(0, 3),
                    AwayScore = _random.Next(0, 3),
                    Status = MatchStatus.Live,
                    StartTime = DateTime.UtcNow.AddMinutes(-_random.Next(1, 45))
                });
            }
            _logger.LogInformation("Initialized {Count} active matches", _activeMatches.Count);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("FeedGeneratorService started - generating high-throughput events");

            // Start metrics reporting task
            _ = Task.Run(() => ReportMetrics(stoppingToken), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await GenerateEventsAsync();
                    await Task.Delay(100, stoppingToken); // 10 iterations/second
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating events");
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }

        private async Task GenerateEventsAsync()
        {
            var tasks = new List<Task>();

            foreach (var match in _activeMatches.Where(m => m.Status == MatchStatus.Live))
            {
                // Simulate match progression
                match.CurrentMinute++;

                if (match.CurrentMinute == 45)
                {
                    tasks.Add(PublishHalfTimeEvent(match));
                }
                else if (match.CurrentMinute == 90)
                {
                    tasks.Add(PublishFullTimeEvent(match));
                    match.Status = MatchStatus.Finished;
                }
                else
                {
                    // Random events with probabilities
                    var eventRoll = _random.NextDouble();

                    if (eventRoll < 0.02) // 2% chance of goal
                    {
                        tasks.Add(PublishGoalEvent(match));
                    }
                    else if (eventRoll < 0.05) // 3% chance of card
                    {
                        tasks.Add(PublishCardEvent(match));
                    }
                    else if (eventRoll < 0.08) // 3% chance of substitution
                    {
                        tasks.Add(PublishSubstitutionEvent(match));
                    }
                    else if (eventRoll < 0.15) // 7% chance of shot
                    {
                        tasks.Add(PublishShotEvent(match));
                    }
                    else if (eventRoll < 0.20) // 5% chance of corner
                    {
                        tasks.Add(PublishCornerEvent(match));
                    }

                    // Update player stats periodically
                    if (match.CurrentMinute % 5 == 0)
                    {
                        tasks.Add(PublishPlayerUpdates(match));
                    }
                }
            }

            // Restart finished matches to keep load constant
            foreach (var match in _activeMatches.Where(m => m.Status == MatchStatus.Finished).ToList())
            {
                match.CurrentMinute = 0;
                match.HomeScore = 0;
                match.AwayScore = 0;
                match.Status = MatchStatus.Live;
                match.StartTime = DateTime.UtcNow;
            }

            await Task.WhenAll(tasks);
        }

        private async Task PublishGoalEvent(MatchSimulation match)
        {
            var isHomeTeam = _random.Next(2) == 0;
            if (isHomeTeam) match.HomeScore++;
            else match.AwayScore++;

            var teamId = isHomeTeam ? match.HomeTeamId : match.AwayTeamId;
            var playerId = (teamId * 11) + _random.Next(1, 12);

            var sportEvent = new SportEventUpdate
            {
                EventId = Guid.NewGuid().ToString(),
                MatchId = match.MatchId,
                EventType = EventType.Goal,
                Minute = match.CurrentMinute,
                PlayerId = playerId,
                TeamId = teamId,
                Description = $"Goal scored by player {playerId}",
                Metadata = new Dictionary<string, object>
                {
                    ["goalType"] = _random.Next(3) switch
                    {
                        0 => "header",
                        1 => "penalty",
                        _ => "regular"
                    },
                    ["homeScore"] = match.HomeScore,
                    ["awayScore"] = match.AwayScore
                },
                Timestamp = DateTime.UtcNow
            };

            await _kafka.PublishAsync("ingest-events", match.MatchId.ToString(), sportEvent);
            Interlocked.Increment(ref _totalEventsGenerated);

            var scoreUpdate = new ScoreUpdate
            {
                MatchId = match.MatchId,
                HomeScore = match.HomeScore,
                AwayScore = match.AwayScore,
                Minute = match.CurrentMinute,
                Status = match.Status,
                Timestamp = DateTime.UtcNow
            };

            await _kafka.PublishAsync("live-scores", match.MatchId.ToString(), scoreUpdate);
            Interlocked.Increment(ref _totalEventsGenerated);

            _logger.LogInformation("⚽ Match {MatchId} - Goal at {Minute}' - Score: {HomeScore}-{AwayScore}",
                match.MatchId, match.CurrentMinute, match.HomeScore, match.AwayScore);
        }

        private async Task PublishCardEvent(MatchSimulation match)
        {
            var teamId = _random.Next(2) == 0 ? match.HomeTeamId : match.AwayTeamId;
            var playerId = (teamId * 11) + _random.Next(1, 12);
            var isYellow = _random.NextDouble() < 0.8;

            var sportEvent = new SportEventUpdate
            {
                EventId = Guid.NewGuid().ToString(),
                MatchId = match.MatchId,
                EventType = EventType.Card,
                Minute = match.CurrentMinute,
                PlayerId = playerId,
                TeamId = teamId,
                Description = $"{(isYellow ? "Yellow" : "Red")} card for player {playerId}",
                Metadata = new Dictionary<string, object>
                {
                    ["cardType"] = isYellow ? "yellow" : "red",
                    ["reason"] = _random.Next(3) switch
                    {
                        0 => "foul",
                        1 => "unsporting_behavior",
                        _ => "dissent"
                    }
                },
                Timestamp = DateTime.UtcNow
            };

            await _kafka.PublishAsync("ingest-events", match.MatchId.ToString(), sportEvent);
            Interlocked.Increment(ref _totalEventsGenerated);

            _logger.LogDebug("🟨 Match {MatchId} - {CardType} card at {Minute}'",
                match.MatchId, isYellow ? "Yellow" : "Red", match.CurrentMinute);
        }

        private async Task PublishSubstitutionEvent(MatchSimulation match)
        {
            var teamId = _random.Next(2) == 0 ? match.HomeTeamId : match.AwayTeamId;
            var playerOut = (teamId * 11) + _random.Next(1, 12);
            var playerIn = (teamId * 11) + _random.Next(12, 20);

            var sportEvent = new SportEventUpdate
            {
                EventId = Guid.NewGuid().ToString(),
                MatchId = match.MatchId,
                EventType = EventType.Substitution,
                Minute = match.CurrentMinute,
                TeamId = teamId,
                Description = $"Substitution: Player {playerIn} in for {playerOut}",
                Metadata = new Dictionary<string, object>
                {
                    ["playerOut"] = playerOut,
                    ["playerIn"] = playerIn
                },
                Timestamp = DateTime.UtcNow
            };

            await _kafka.PublishAsync("ingest-events", match.MatchId.ToString(), sportEvent);
            Interlocked.Increment(ref _totalEventsGenerated);
        }

        private async Task PublishShotEvent(MatchSimulation match)
        {
            var teamId = _random.Next(2) == 0 ? match.HomeTeamId : match.AwayTeamId;
            var playerId = (teamId * 11) + _random.Next(1, 12);
            var onTarget = _random.Next(2) == 0;

            var sportEvent = new SportEventUpdate
            {
                EventId = Guid.NewGuid().ToString(),
                MatchId = match.MatchId,
                EventType = EventType.Shot,
                Minute = match.CurrentMinute,
                PlayerId = playerId,
                TeamId = teamId,
                Description = $"Shot by player {playerId}",
                Metadata = new Dictionary<string, object>
                {
                    ["onTarget"] = onTarget,
                    ["bodyPart"] = _random.Next(3) switch
                    {
                        0 => "right_foot",
                        1 => "left_foot",
                        _ => "header"
                    }
                },
                Timestamp = DateTime.UtcNow
            };

            await _kafka.PublishAsync("ingest-events", match.MatchId.ToString(), sportEvent);
            Interlocked.Increment(ref _totalEventsGenerated);
        }

        private async Task PublishCornerEvent(MatchSimulation match)
        {
            var teamId = _random.Next(2) == 0 ? match.HomeTeamId : match.AwayTeamId;

            var sportEvent = new SportEventUpdate
            {
                EventId = Guid.NewGuid().ToString(),
                MatchId = match.MatchId,
                EventType = EventType.Corner,
                Minute = match.CurrentMinute,
                TeamId = teamId,
                Description = $"Corner for team {teamId}",
                Metadata = new Dictionary<string, object>
                {
                    ["side"] = _random.Next(2) == 0 ? "left" : "right"
                },
                Timestamp = DateTime.UtcNow
            };

            await _kafka.PublishAsync("ingest-events", match.MatchId.ToString(), sportEvent);
            Interlocked.Increment(ref _totalEventsGenerated);
        }

        private async Task PublishHalfTimeEvent(MatchSimulation match)
        {
            match.Status = MatchStatus.HalfTime;

            var sportEvent = new SportEventUpdate
            {
                EventId = Guid.NewGuid().ToString(),
                MatchId = match.MatchId,
                EventType = EventType.HalfTime,
                Minute = 45,
                Description = "Half Time",
                Metadata = new Dictionary<string, object>
                {
                    ["homeScore"] = match.HomeScore,
                    ["awayScore"] = match.AwayScore
                },
                Timestamp = DateTime.UtcNow
            };

            await _kafka.PublishAsync("ingest-events", match.MatchId.ToString(), sportEvent);
            Interlocked.Increment(ref _totalEventsGenerated);

            _logger.LogInformation("⏸️ Match {MatchId} - Half Time - Score: {HomeScore}-{AwayScore}",
                match.MatchId, match.HomeScore, match.AwayScore);

            // Resume after 5 seconds (simulated half-time break)
            await Task.Delay(5000);
            match.Status = MatchStatus.Live;
            match.CurrentMinute = 45;
        }

        private async Task PublishFullTimeEvent(MatchSimulation match)
        {
            var sportEvent = new SportEventUpdate
            {
                EventId = Guid.NewGuid().ToString(),
                MatchId = match.MatchId,
                EventType = EventType.FullTime,
                Minute = 90,
                Description = "Full Time",
                Metadata = new Dictionary<string, object>
                {
                    ["finalHomeScore"] = match.HomeScore,
                    ["finalAwayScore"] = match.AwayScore
                },
                Timestamp = DateTime.UtcNow
            };

            await _kafka.PublishAsync("ingest-events", match.MatchId.ToString(), sportEvent);
            Interlocked.Increment(ref _totalEventsGenerated);

            _logger.LogInformation("🏁 Match {MatchId} - Full Time - Final Score: {HomeScore}-{AwayScore}",
                match.MatchId, match.HomeScore, match.AwayScore);
        }

        private async Task PublishPlayerUpdates(MatchSimulation match)
        {
            // Update stats for 4 random players from both teams
            for (int i = 0; i < 4; i++)
            {
                var teamId = _random.Next(2) == 0 ? match.HomeTeamId : match.AwayTeamId;
                var playerId = (teamId * 11) + _random.Next(1, 12);

                var playerUpdate = new PlayerUpdate
                {
                    PlayerId = playerId,
                    MatchId = match.MatchId,
                    Statistics = new Dictionary<string, object>
                    {
                        ["shots"] = _random.Next(0, 5),
                        ["shotsOnTarget"] = _random.Next(0, 3),
                        ["passes"] = _random.Next(10, 50),
                        ["passAccuracy"] = _random.Next(70, 95),
                        ["tackles"] = _random.Next(0, 8),
                        ["interceptions"] = _random.Next(0, 5),
                        ["fouls"] = _random.Next(0, 3),
                        ["minutesPlayed"] = match.CurrentMinute
                    },
                    Timestamp = DateTime.UtcNow
                };

                await _kafka.PublishAsync("player-updates", playerId.ToString(), playerUpdate);
                Interlocked.Increment(ref _totalEventsGenerated);
            }
        }

        private async Task ReportMetrics(CancellationToken stoppingToken)
        {
            var lastCount = 0L;

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(10000, stoppingToken); // Report every 10 seconds

                var currentCount = Interlocked.Read(ref _totalEventsGenerated);
                var eventsPerSecond = (currentCount - lastCount) / 10.0;
                lastCount = currentCount;

                _logger.LogInformation(
                    "📊 Metrics - Total Events: {TotalEvents:N0} | Events/sec: {EventsPerSec:N1} | Active Matches: {ActiveMatches}",
                    currentCount,
                    eventsPerSecond,
                    _activeMatches.Count(m => m.Status == MatchStatus.Live));
            }
        }
    }
}