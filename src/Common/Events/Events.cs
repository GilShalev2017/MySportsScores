using Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Events
{
    public class SportEventUpdate
    {
        public string EventId { get; set; }
        public int MatchId { get; set; }
        public EventType EventType { get; set; }
        public int Minute { get; set; }
        public int? PlayerId { get; set; }
        public int? TeamId { get; set; }
        public string Description { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
        public DateTime Timestamp { get; set; }
    }
    
    public class ScoreUpdate
    {
        public int MatchId { get; set; }
        public int HomeScore { get; set; }
        public int AwayScore { get; set; }
        public int Minute { get; set; }
        public MatchStatus Status { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class PlayerUpdate
    {
        public int PlayerId { get; set; }
        public int MatchId { get; set; }
        public Dictionary<string, object> Statistics { get; set; }
        public DateTime Timestamp { get; set; }
    }
    
    public class MatchStatusChange
    {
        public int MatchId { get; set; }
        public MatchStatus OldStatus { get; set; }
        public MatchStatus NewStatus { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
