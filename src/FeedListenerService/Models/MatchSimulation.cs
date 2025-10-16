using Common.Models;

namespace FeedListenerService.Models
{
    public class MatchSimulation
    {
        public int MatchId { get; set; }
        public int HomeTeamId { get; set; }
        public int AwayTeamId { get; set; }
        public int LeagueId { get; set; }
        public int CurrentMinute { get; set; }
        public int HomeScore { get; set; }
        public int AwayScore { get; set; }
        public MatchStatus Status { get; set; }
        public DateTime StartTime { get; set; }
    }
}
