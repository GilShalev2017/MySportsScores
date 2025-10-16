using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs
{
    public class LiveMatchDto
    {
        public int MatchId { get; set; }
        public string HomeTeam { get; set; }
        public string AwayTeam { get; set; }
        public int HomeScore { get; set; }
        public int AwayScore { get; set; }
        public int Minute { get; set; }
        public string Status { get; set; }
        public string League { get; set; }
    }

    public class PlayerStatsDto
    {
        public int PlayerId { get; set; }
        public string PlayerName { get; set; }
        public string Team { get; set; }
        public int Goals { get; set; }
        public int Assists { get; set; }
        public int YellowCards { get; set; }
        public int RedCards { get; set; }
        public int MinutesPlayed { get; set; }
    }
}
