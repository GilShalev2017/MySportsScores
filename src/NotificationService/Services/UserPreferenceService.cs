using Common.Models;
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
        Task AddTeamPreferenceAsync(string userId, int teamId);
        Task RemoveTeamPreferenceAsync(string userId, int teamId);
        Task AddPlayerPreferenceAsync(string userId, int playerId);
        Task RemovePlayerPreferenceAsync(string userId, int playerId);
        Task AddLeaguePreferenceAsync(string userId, int leagueId);
        Task RemoveLeaguePreferenceAsync(string userId, int leagueId);
        Task<UserPreference> GetUserPreferencesAsync(string userId);
        Task RemoveUserAsync(string userId);
        Task<List<string>> GetUsersInterestedInTeamAsync(int teamId);
        Task<List<string>> GetUsersInterestedInPlayerAsync(int playerId);
    }

    public class UserPreferenceService : IUserPreferenceService
    {
        private readonly ConcurrentDictionary<string, UserPreference> _userPreferences = new();
        private readonly ILogger<UserPreferenceService> _logger;

        public UserPreferenceService(ILogger<UserPreferenceService> logger)
        {
            _logger = logger;
        }

        public Task AddTeamPreferenceAsync(string userId, int teamId)
        {
            var preference = _userPreferences.GetOrAdd(userId, _ => new UserPreference
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            });

            if (!preference.FavoriteTeamIds.Contains(teamId))
            {
                preference.FavoriteTeamIds.Add(teamId);
                preference.UpdatedAt = DateTime.UtcNow;
            }

            return Task.CompletedTask;
        }

        public Task RemoveTeamPreferenceAsync(string userId, int teamId)
        {
            if (_userPreferences.TryGetValue(userId, out var preference))
            {
                preference.FavoriteTeamIds.Remove(teamId);
                preference.UpdatedAt = DateTime.UtcNow;
            }
            return Task.CompletedTask;
        }

        public Task AddPlayerPreferenceAsync(string userId, int playerId)
        {
            var preference = _userPreferences.GetOrAdd(userId, _ => new UserPreference
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            });

            if (!preference.FavoritePlayerIds.Contains(playerId))
            {
                preference.FavoritePlayerIds.Add(playerId);
                preference.UpdatedAt = DateTime.UtcNow;
            }

            return Task.CompletedTask;
        }

        public Task RemovePlayerPreferenceAsync(string userId, int playerId)
        {
            if (_userPreferences.TryGetValue(userId, out var preference))
            {
                preference.FavoritePlayerIds.Remove(playerId);
                preference.UpdatedAt = DateTime.UtcNow;
            }
            return Task.CompletedTask;
        }

        public Task AddLeaguePreferenceAsync(string userId, int leagueId)
        {
            var preference = _userPreferences.GetOrAdd(userId, _ => new UserPreference
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            });

            if (!preference.FavoriteLeagueIds.Contains(leagueId))
            {
                preference.FavoriteLeagueIds.Add(leagueId);
                preference.UpdatedAt = DateTime.UtcNow;
            }

            return Task.CompletedTask;
        }

        public Task RemoveLeaguePreferenceAsync(string userId, int leagueId)
        {
            if (_userPreferences.TryGetValue(userId, out var preference))
            {
                preference.FavoriteLeagueIds.Remove(leagueId);
                preference.UpdatedAt = DateTime.UtcNow;
            }
            return Task.CompletedTask;
        }

        public Task<UserPreference> GetUserPreferencesAsync(string userId)
        {
            if (_userPreferences.TryGetValue(userId, out var preference))
            {
                return Task.FromResult(preference);
            }

            return Task.FromResult(new UserPreference
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            });
        }

        public Task RemoveUserAsync(string userId)
        {
            _userPreferences.TryRemove(userId, out _);
            return Task.CompletedTask;
        }

        public Task<List<string>> GetUsersInterestedInTeamAsync(int teamId)
        {
            var users = _userPreferences
                .Where(kvp => kvp.Value.FavoriteTeamIds.Contains(teamId))
                .Select(kvp => kvp.Key)
                .ToList();

            return Task.FromResult(users);
        }

        public Task<List<string>> GetUsersInterestedInPlayerAsync(int playerId)
        {
            var users = _userPreferences
                .Where(kvp => kvp.Value.FavoritePlayerIds.Contains(playerId))
                .Select(kvp => kvp.Key)
                .ToList();

            return Task.FromResult(users);
        }
    }

}
