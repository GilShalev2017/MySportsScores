using Common.Models;
using Microsoft.AspNetCore.SignalR;
using NotificationService.Services;

namespace NotificationService.Hubs
{
    public class SportsNotificationHub : Hub
    {
        private readonly IUserPreferenceService _userPreferenceService;
        private readonly ILogger<SportsNotificationHub> _logger;

        public SportsNotificationHub(
            IUserPreferenceService userPreferenceService,
            ILogger<SportsNotificationHub> logger)
        {
            _userPreferenceService = userPreferenceService;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.ConnectionId; // Or use real user ID from JWT

            _logger.LogInformation("Client connected: {ConnectionId}, restoring subscriptions...", userId);

            try
            {
                // *** LOAD PREFERENCES FROM SQL ***
                var preferences = await _userPreferenceService.GetUserPreferencesAsync(userId);

                // *** RESTORE ALL SIGNALR GROUP MEMBERSHIPS ***
                foreach (var teamId in preferences.FavoriteTeamIds)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"team_{teamId}");
                    _logger.LogDebug("Restored subscription to team {TeamId} for user {UserId}", teamId, userId);
                }

                foreach (var playerId in preferences.FavoritePlayerIds)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"player_{playerId}");
                    _logger.LogDebug("Restored subscription to player {PlayerId} for user {UserId}", playerId, userId);
                }

                foreach (var leagueId in preferences.FavoriteLeagueIds)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"league_{leagueId}");
                    _logger.LogDebug("Restored subscription to league {LeagueId} for user {UserId}", leagueId, userId);
                }

                // Notify client that subscriptions are restored
                await Clients.Caller.SendAsync("SubscriptionsRestored", new
                {
                    teams = preferences.FavoriteTeamIds,
                    players = preferences.FavoritePlayerIds,
                    leagues = preferences.FavoriteLeagueIds
                });

                _logger.LogInformation("✅ Restored {TeamCount} teams, {PlayerCount} players, {LeagueCount} leagues for user {UserId}",
                    preferences.FavoriteTeamIds.Count,
                    preferences.FavoritePlayerIds.Count,
                    preferences.FavoriteLeagueIds.Count,
                    userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring subscriptions for user {UserId}", userId);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);

            // Clean up user preferences
            await _userPreferenceService.RemoveUserAsync(Context.ConnectionId);

            await base.OnDisconnectedAsync(exception);
        }

        public async Task SubscribeToTeam(int teamId)
        {
            var userId = Context.ConnectionId;

            // 1. Persist to SQL
            await _userPreferenceService.AddFavoriteTeamAsync(userId, teamId);

            // 2. Join SignalR group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"team_{teamId}");

            _logger.LogInformation("User {UserId} subscribed to team {TeamId}", userId, teamId);

            await Clients.Caller.SendAsync("SubscriptionConfirmed", new { type = "team", id = teamId });
        }

        public async Task UnsubscribeFromTeam(int teamId)
        {
            var userId = Context.ConnectionId;

            // 1. Remove from SQL
            await _userPreferenceService.RemoveFavoriteTeamAsync(userId, teamId);

            // 2. Leave SignalR group
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"team_{teamId}");

            _logger.LogInformation("User {UserId} unsubscribed from team {TeamId}", userId, teamId);

            await Clients.Caller.SendAsync("UnsubscriptionConfirmed", new { type = "team", id = teamId });
        }

        public async Task SubscribeToPlayer(int playerId)
        {
            var userId = Context.ConnectionId;

            // 1. Persist to SQL
            await _userPreferenceService.AddFavoritePlayerAsync(userId, playerId);

            // 2. Join SignalR group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"player_{playerId}");

            _logger.LogInformation("User {UserId} subscribed to player {PlayerId}", userId, playerId);

            await Clients.Caller.SendAsync("SubscriptionConfirmed", new { type = "player", id = playerId });
        }

        public async Task UnsubscribeFromPlayer(int playerId)
        {
            var userId = Context.ConnectionId;

            // 1. Remove from SQL
            await _userPreferenceService.RemoveFavoritePlayerAsync(userId, playerId);

            // 2. Leave SignalR group
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"player_{playerId}");

            _logger.LogInformation("User {UserId} unsubscribed from player {PlayerId}", userId, playerId);

            await Clients.Caller.SendAsync("UnsubscriptionConfirmed", new { type = "player", id = playerId });
        }

        public async Task SubscribeToLeague(int leagueId)
        {
            var userId = Context.ConnectionId;

            // 1. Persist to SQL
            await _userPreferenceService.AddFavoriteLeagueAsync(userId, leagueId);

            // 2. Join SignalR group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"league_{leagueId}");

            _logger.LogInformation("User {UserId} subscribed to league {LeagueId}", userId, leagueId);

            await Clients.Caller.SendAsync("SubscriptionConfirmed", new { type = "league", id = leagueId });
        }

        public async Task UnsubscribeFromLeague(int leagueId)
        {
            var userId = Context.ConnectionId;

            // 1. Remove from SQL
            await _userPreferenceService.RemoveFavoriteLeagueAsync(userId, leagueId);

            // 2. Leave SignalR group
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"league_{leagueId}");

            _logger.LogInformation("User {UserId} unsubscribed from league {LeagueId}", userId, leagueId);

            await Clients.Caller.SendAsync("UnsubscriptionConfirmed", new { type = "league", id = leagueId });
        }

        public async Task SubscribeToMatch(int matchId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"match_{matchId}");
            _logger.LogInformation("User {UserId} subscribed to match {MatchId}", Context.ConnectionId, matchId);
            await Clients.Caller.SendAsync("SubscriptionConfirmed", new { type = "match", id = matchId });
        }

        public async Task UnsubscribeFromMatch(int matchId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"match_{matchId}");
            _logger.LogInformation("User {UserId} unsubscribed from match {MatchId}", Context.ConnectionId, matchId);
        }

        public async Task GetUserPreferences()
        {
            var userId = Context.ConnectionId;
            var preferences = await _userPreferenceService.GetUserPreferencesAsync(userId);
            await Clients.Caller.SendAsync("UserPreferences", preferences);
        }
    }
}
