using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Models
{
    public enum SportType
    {
        Football = 1,
        Basketball = 2,
        Tennis = 3,
        Cricket = 4
    }
    
    public class League
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public SportType Sport { get; set; }
        public string Country { get; set; }
        public int Season { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class Team
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ShortName { get; set; }
        public string LogoUrl { get; set; }
        public string Country { get; set; }
        public int LeagueId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
    
    public class Player
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName => $"{FirstName} {LastName}";
        public int ShirtNumber { get; set; }
        public string Position { get; set; }
        public int TeamId { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Nationality { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    
    public enum MatchStatus
    {
        Scheduled = 0,
        Live = 1,
        HalfTime = 2,
        Finished = 3,
        Postponed = 4,
        Cancelled = 5
    }

    public class Match
    {
        public int Id { get; set; }
        public int LeagueId { get; set; }
        public int HomeTeamId { get; set; }
        public int AwayTeamId { get; set; }
        public DateTime ScheduledTime { get; set; }
        public MatchStatus Status { get; set; }
        public int HomeScore { get; set; }
        public int AwayScore { get; set; }
        public int Minute { get; set; }
        public string Venue { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    
    public enum EventType
    {
        MatchStart = 1,
        Goal = 2,
        Card = 3,
        Substitution = 4,
        HalfTime = 5,
        FullTime = 6,
        Assist = 7,
        Shot = 8,
        Corner = 9,
        Foul = 10
    }

    public class SportEvent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public int MatchId { get; set; }
        public EventType Type { get; set; }
        public int Minute { get; set; }
        public int? PlayerId { get; set; }
        public int? TeamId { get; set; }
        public string Description { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class UserPreference
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public List<int> FavoriteTeamIds { get; set; } = new();
        public List<int> FavoritePlayerIds { get; set; } = new();
        public List<int> FavoriteLeagueIds { get; set; } = new();
        public List<EventType> EventTypesToNotify { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

}
