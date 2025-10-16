using Microsoft.AspNetCore.SignalR;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
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
            await _userPreferenceService.AddTeamPreferenceAsync(userId, teamId);
            await Groups.AddToGroupAsync(Context.ConnectionId, $"team_{teamId}");

            _logger.LogInformation("User {UserId} subscribed to team {TeamId}", userId, teamId);
            await Clients.Caller.SendAsync("SubscriptionConfirmed", new { type = "team", id = teamId });
        }

        public async Task UnsubscribeFromTeam(int teamId)
        {
            var userId = Context.ConnectionId;
            await _userPreferenceService.RemoveTeamPreferenceAsync(userId, teamId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"team_{teamId}");

            _logger.LogInformation("User {UserId} unsubscribed from team {TeamId}", userId, teamId);
        }

        public async Task SubscribeToPlayer(int playerId)
        {
            var userId = Context.ConnectionId;
            await _userPreferenceService.AddPlayerPreferenceAsync(userId, playerId);
            await Groups.AddToGroupAsync(Context.ConnectionId, $"player_{playerId}");

            _logger.LogInformation("User {UserId} subscribed to player {PlayerId}", userId, playerId);
            await Clients.Caller.SendAsync("SubscriptionConfirmed", new { type = "player", id = playerId });
        }

        public async Task UnsubscribeFromPlayer(int playerId)
        {
            var userId = Context.ConnectionId;
            await _userPreferenceService.RemovePlayerPreferenceAsync(userId, playerId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"player_{playerId}");

            _logger.LogInformation("User {UserId} unsubscribed from player {PlayerId}", userId, playerId);
        }

        public async Task SubscribeToLeague(int leagueId)
        {
            var userId = Context.ConnectionId;
            await _userPreferenceService.AddLeaguePreferenceAsync(userId, leagueId);
            await Groups.AddToGroupAsync(Context.ConnectionId, $"league_{leagueId}");

            _logger.LogInformation("User {UserId} subscribed to league {LeagueId}", userId, leagueId);
            await Clients.Caller.SendAsync("SubscriptionConfirmed", new { type = "league", id = leagueId });
        }

        public async Task UnsubscribeFromLeague(int leagueId)
        {
            var userId = Context.ConnectionId;
            await _userPreferenceService.RemoveLeaguePreferenceAsync(userId, leagueId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"league_{leagueId}");

            _logger.LogInformation("User {UserId} unsubscribed from league {LeagueId}", userId, leagueId);
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
