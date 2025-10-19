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
        public required string Name { get; set; }
        public SportType Sport { get; set; }
        public required string Country { get; set; }
        public required int Season { get; set; }
        public required DateTime CreatedAt { get; set; }
        public required DateTime UpdatedAt { get; set; }
    }

    public class Team
    {
        public required int Id { get; set; }
        public required string Name { get; set; }
        public required string ShortName { get; set; }
        public required string LogoUrl { get; set; }
        public required string Country { get; set; }
        public required int LeagueId { get; set; }
        public required DateTime CreatedAt { get; set; }
        public required DateTime UpdatedAt { get; set; }
    }
    
    public class Player
    {
        public required int Id { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public string FullName => $"{FirstName} {LastName}";
        public required int ShirtNumber { get; set; }
        public required string Position { get; set; }
        public required int TeamId { get; set; }
        public required DateTime DateOfBirth { get; set; }
        public required string Nationality { get; set; }
        public required DateTime CreatedAt { get; set; }
        public required DateTime UpdatedAt { get; set; }
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
        public required int Id { get; set; }
        public required int LeagueId { get; set; }
        public required int HomeTeamId { get; set; }
        public required int AwayTeamId { get; set; }
        public required DateTime ScheduledTime { get; set; }
        public required MatchStatus Status { get; set; }
        public required int HomeScore { get; set; }
        public required int AwayScore { get; set; }
        public required int Minute { get; set; }
        public required string Venue { get; set; }
        public required DateTime CreatedAt { get; set; }
        public required DateTime UpdatedAt { get; set; }
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
        public required string Description { get; set; }
        public required Dictionary<string, object> Metadata { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class UserPreference
    {
        public int Id { get; set; }
        public required string UserId { get; set; }
        public List<int> FavoriteTeamIds { get; set; } = new();
        public List<int> FavoritePlayerIds { get; set; } = new();
        public List<int> FavoriteLeagueIds { get; set; } = new();
        public List<EventType> EventTypesToNotify { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

}
