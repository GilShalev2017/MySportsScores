using Common.Data;
using Common.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationService.Services
{
    public interface IUserPreferenceService
    {
        Task AddFavoriteTeamAsync(string userId, int teamId);
        Task RemoveFavoriteTeamAsync(string userId, int teamId);
        Task AddFavoritePlayerAsync(string userId, int playerId);
        Task RemoveFavoritePlayerAsync(string userId, int playerId);
        Task AddFavoriteLeagueAsync(string userId, int leagueId);
        Task RemoveFavoriteLeagueAsync(string userId, int leagueId);
        Task<UserPreference> GetUserPreferencesAsync(string userId);
        Task RemoveUserAsync(string userId);
        Task<List<string>> GetUsersInterestedInTeamAsync(int teamId);
        Task<List<string>> GetUsersInterestedInPlayerAsync(int playerId);
    }

    public class UserPreferenceService : IUserPreferenceService
    {
        private readonly ILogger<UserPreferenceService> _logger;
        private readonly SportsDbContext _dbContext;

        public UserPreferenceService(SportsDbContext dbContext, ILogger<UserPreferenceService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task AddFavoriteTeamAsync(string userId, int teamId)
        {
            var preferences = await GetUserPreferencesAsync(userId);

            if (!preferences.FavoriteTeamIds.Contains(teamId))
            {
                preferences.FavoriteTeamIds.Add(teamId);
                preferences.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("User {UserId} subscribed to team {TeamId}", userId, teamId);
            }
        }

        public async Task RemoveFavoriteTeamAsync(string userId, int teamId)
        {
            var preferences = await GetUserPreferencesAsync(userId);

            if (preferences.FavoriteTeamIds.Remove(teamId))
            {
                preferences.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("User {UserId} unsubscribed from team {TeamId}", userId, teamId);
            }
        }

        public async Task AddFavoritePlayerAsync(string userId, int playerId)
        {
            var preferences = await GetUserPreferencesAsync(userId);

            if (!preferences.FavoritePlayerIds.Contains(playerId))
            {
                preferences.FavoritePlayerIds.Add(playerId);
                preferences.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("User {UserId} subscribed to player {PlayerId}", userId, playerId);
            }
        }

        public async Task RemoveFavoritePlayerAsync(string userId, int playerId)
        {
            var preferences = await GetUserPreferencesAsync(userId);

            if (preferences.FavoritePlayerIds.Remove(playerId))
            {
                preferences.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("User {UserId} unsubscribed from player {PlayerId}", userId, playerId);
            }
        }

        public async Task AddFavoriteLeagueAsync(string userId, int leagueId)
        {
            //var preference = _userPreferences.GetOrAdd(userId, _ => new UserPreference
            //{
            //    UserId = userId,
            //    CreatedAt = DateTime.UtcNow
            //});

            //if (!preference.FavoriteLeagueIds.Contains(leagueId))
            //{
            //    preference.FavoriteLeagueIds.Add(leagueId);
            //    preference.UpdatedAt = DateTime.UtcNow;
            //}

            //return Task.CompletedTask;
            var preferences = await GetUserPreferencesAsync(userId);

            if (!preferences.FavoriteLeagueIds.Contains(leagueId))
            {
                preferences.FavoriteLeagueIds.Add(leagueId);
                preferences.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("User {UserId} subscribed to league {LeagueId}", userId, leagueId);
            }
        }

        public async Task RemoveFavoriteLeagueAsync(string userId, int leagueId)
        {
            //if (_userPreferences.TryGetValue(userId, out var preference))
            //{
            //    preference.FavoriteLeagueIds.Remove(leagueId);
            //    preference.UpdatedAt = DateTime.UtcNow;
            //}
            //return Task.CompletedTask;
            var preferences = await GetUserPreferencesAsync(userId);

            if (preferences.FavoriteLeagueIds.Remove(leagueId))
            {
                preferences.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("User {UserId} unsubscribed from league {LeagueId}", userId, leagueId);
            }
        }

        public async Task<UserPreference> GetUserPreferencesAsync(string userId)
        {
            var preferences = await _dbContext.UserPreferences.FirstOrDefaultAsync(p => p.UserId == userId);

            if (preferences == null)
            {
                // Create default preferences for new user
                preferences = new UserPreference
                {
                    UserId = userId,
                    FavoriteTeamIds = new List<int>(),
                    FavoritePlayerIds = new List<int>(),
                    FavoriteLeagueIds = new List<int>(),
                    EventTypesToNotify = new List<EventType>
                {
                    EventType.Goal,
                    EventType.Card,
                    EventType.MatchStart,
                    EventType.FullTime
                },
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _dbContext.UserPreferences.Add(preferences);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Created new user preferences for {UserId}", userId);
            }

            return preferences;
        }

        public async Task RemoveUserAsync(string userId)
        {
            var preferences = await _dbContext.UserPreferences.FirstOrDefaultAsync(p => p.UserId == userId);

            if (preferences != null)
            {
                _dbContext.UserPreferences.Remove(preferences);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Removed user preferences for {UserId}", userId);
            }
        }

        public async Task<List<string>> GetUsersInterestedInTeamAsync(int teamId)
        {
            var users = await _dbContext.UserPreferences
                     .Where(p => p.FavoriteTeamIds.Contains(teamId))
                     .Select(p => p.UserId)
                     .ToListAsync();

            return users;
        }

        public async Task<List<string>> GetUsersInterestedInPlayerAsync(int playerId)
        {
            var users = await _dbContext.UserPreferences
                       .Where(p => p.FavoritePlayerIds.Contains(playerId))
                       .Select(p => p.UserId)
                       .ToListAsync();

            return users;
        }
    }

}
