//using Microsoft.EntityFrameworkCore;
//using Common.Data;
//using Common.Models;

//namespace IngestService.Seeding
//{
//    public static class DataSeeder
//    {
//        public static async Task SeedDatabaseAsync(IServiceProvider services)
//        {
//            using var scope = services.CreateScope();
//            var context = scope.ServiceProvider.GetRequiredService<SportsDbContext>();

//            // 1. Check and create League
//            if (!await context.Leagues.AnyAsync())
//            {
//                var league = new League
//                {
//                    // REMOVE: Id = 1,
//                    Name = "Premier League",
//                    Sport = SportType.Football,
//                    Country = "England",
//                    Season = 2024,
//                    CreatedAt = DateTime.UtcNow,
//                    UpdatedAt = DateTime.UtcNow
//                };
//                context.Leagues.Add(league);
//                // League is now tracked, but ID is not yet assigned.

//                // 2. Check and create Teams
//                if (!await context.Teams.AnyAsync())
//                {
//                    var team1 = new Team
//                    {
//                        // REMOVE: Id = 1,
//                        Name = "Manchester United",
//                        ShortName = "MANU",
//                        LogoUrl = "https://example.com/manu-logo.png",
//                        Country = "England",
//                        // ASSIGN BY REFERENCE, NOT HARDCODED ID
//                        LeagueId = league.Id, // EF Core will resolve this upon save.
//                        CreatedAt = DateTime.UtcNow,
//                        UpdatedAt = DateTime.UtcNow
//                    };

//                    var team2 = new Team
//                    {
//                        // REMOVE: Id = 2,
//                        Name = "Liverpool FC",
//                        ShortName = "LIV",
//                        LogoUrl = "https://example.com/liverpool-logo.png",
//                        Country = "England",
//                        // ASSIGN BY REFERENCE, NOT HARDCODED ID
//                        LeagueId = league.Id,
//                        CreatedAt = DateTime.UtcNow,
//                        UpdatedAt = DateTime.UtcNow
//                    };
//                    context.Teams.AddRange(team1, team2);

//                    // 3. Check and create Players
//                    if (!await context.Players.AnyAsync())
//                    {
//                        var player = new Player
//                        {
//                            // REMOVE: Id = 1,
//                            FirstName = "Mohamed",
//                            LastName = "Salah",
//                            ShirtNumber = 11,
//                            Position = "Forward",
//                            // ASSIGN BY REFERENCE, NOT HARDCODED ID
//                            TeamId = team2.Id,
//                            DateOfBirth = new DateTime(1992, 6, 15),
//                            Nationality = "Egypt",
//                            CreatedAt = DateTime.UtcNow,
//                            UpdatedAt = DateTime.UtcNow
//                        };
//                        context.Players.Add(player);
//                    }

//                    // 4. Check and create Matches
//                    if (!await context.Matches.AnyAsync())
//                    {
//                        var match = new Match
//                        {
//                            // REMOVE: Id = 1,
//                            // ASSIGN BY REFERENCE
//                            LeagueId = league.Id,
//                            HomeTeamId = team1.Id,
//                            AwayTeamId = team2.Id,
//                            ScheduledTime = DateTime.UtcNow.AddHours(5),
//                            Status = MatchStatus.Scheduled,
//                            HomeScore = 0,
//                            AwayScore = 0,
//                            Minute = 0,
//                            Venue = "Old Trafford",
//                            CreatedAt = DateTime.UtcNow,
//                            UpdatedAt = DateTime.UtcNow
//                        };
//                        context.Matches.Add(match);
//                    }

//                    await context.SaveChangesAsync();
//                }
//            }
//        }
//    }
//}
using Microsoft.EntityFrameworkCore;
using Common.Data;
using Common.Models;
using System.Collections.Generic;
using System.Linq;

namespace IngestService.Seeding
{
    public static class DataSeeder
    {
        private static readonly Random _random = new Random();

        public static async Task SeedDatabaseAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<SportsDbContext>();

            // Ensure the database is created and migrations are applied
            await context.Database.MigrateAsync();

            if (!await context.Leagues.AnyAsync())
            {
                var now = DateTime.UtcNow;
                var teams = new List<Team>();
                var players = new List<Player>();
                var matches = new List<Match>();

                // --- 1. Create 5 Leagues (Matching FeedGenerator's distribution logic: (i % 5) + 1) ---
                var leagues = new List<League>
                {
                    new League { Name = "Premier League", Sport = SportType.Football, Country = "England", Season = 2024, CreatedAt = now, UpdatedAt = now },
                    new League { Name = "La Liga", Sport = SportType.Football, Country = "Spain", Season = 2024, CreatedAt = now, UpdatedAt = now },
                    new League { Name = "NBA", Sport = SportType.Basketball, Country = "USA", Season = 2024, CreatedAt = now, UpdatedAt = now },
                    new League { Name = "Wimbledon Tennis", Sport = SportType.Tennis, Country = "UK", Season = 2024, CreatedAt = now, UpdatedAt = now },
                    new League { Name = "Bundesliga", Sport = SportType.Football, Country = "Germany", Season = 2024, CreatedAt = now, UpdatedAt = now }
                };
                context.Leagues.AddRange(leagues);

                // Save immediately to retrieve generated IDs (1, 2, 3, 4, 5) for use in subsequent entities
                await context.SaveChangesAsync();


                // --- 2. Create 40 Teams (2 teams * 20 matches = 40 unique teams) ---
                var footballTeamNames = new string[] { "Man Utd", "Liverpool", "Chelsea", "Arsenal", "Spurs", "Newcastle", "Everton", "West Ham",
                                                       "Real Madrid", "Barcelona", "Atletico", "Valencia", "Bayern", "Dortmund", "Leverkusen", "Leipzig" };

                var teamCounter = 0;
                var currentLeagueIndex = 0;

                // Create 40 teams, distributed across the 5 leagues
                for (int i = 0; i < 40; i++)
                {
                    var currentLeague = leagues[currentLeagueIndex];
                    var name = i < footballTeamNames.Length ? footballTeamNames[i] : $"Team {i + 1} ({currentLeague.Sport})";

                    var team = new Team
                    {
                        Name = name,
                        ShortName = name.Substring(0, Math.Min(4, name.Length)).ToUpper().Replace(" ", ""),
                        LogoUrl = $"https://example.com/{name.ToLower().Replace(" ", "-")}-logo.png",
                        Country = currentLeague.Country,
                        LeagueId = currentLeague.Id, // Use the actual generated League ID
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    teams.Add(team);

                    // Cycle through the leagues for distribution
                    currentLeagueIndex = (currentLeagueIndex + 1) % leagues.Count;
                    teamCounter++;
                }
                context.Teams.AddRange(teams);

                // Save immediately to retrieve generated IDs (1 to 40) for player assignment
                await context.SaveChangesAsync();


                // --- 3. Create Players (18 players per team: 11 starters + 7 bench) ---
                var positions = new string[] { "GK", "DF", "DF", "DF", "DF", "MF", "MF", "MF", "FW", "FW", "FW",
                                               "SUB_GK", "SUB_DF", "SUB_MF", "SUB_FW", "SUB", "SUB", "SUB" };

                var firstNames = new string []{ "Mohamed", "Erling", "Virgil", "Kevin", "Lionel", "Marcus", "Bukayo", "Son", "Trent", "Bruno" };
                var lastNames = new string []{ "Salah", "Haaland", "Van Dijk", "De Bruyne", "Messi", "Rashford", "Saka", "Heung-min", "Alexander-Arnold", "Fernandes" };

                foreach (var team in teams)
                {
                    // Generate 18 players per team
                    for (int i = 0; i < 18; i++)
                    {
                        var position = positions[i];

                        var player = new Player
                        {
                            FirstName = firstNames[_random.Next(firstNames.Length)],
                            LastName = lastNames[_random.Next(lastNames.Length)],
                            ShirtNumber = i + 1,
                            Position = position,
                            TeamId = team.Id, // Use the actual generated Team ID
                            DateOfBirth = DateTime.UtcNow.AddYears(-_random.Next(20, 35)),
                            Nationality = team.Country,
                            CreatedAt = now,
                            UpdatedAt = now
                        };
                        players.Add(player);
                    }
                }
                context.Players.AddRange(players);


                // --- 4. Create Matches (20 Matches as expected by FeedGenerator) ---

                // Fetch the actual teams (now including their generated IDs)
                var seededTeams = teams.ToList();

                for (int i = 1; i <= 20; i++)
                {
                    // Match generator expects Team IDs 1, 2, 3, 4,... 40. 
                    // We rely on the teams being inserted sequentially to match this.
                    var homeTeam = seededTeams[(i * 2) - 2];
                    var awayTeam = seededTeams[(i * 2) - 1];

                    // Match generator expects League IDs 1-5
                    var matchLeague = leagues[(i - 1) % 5];

                    var match = new Match
                    {
                        LeagueId = matchLeague.Id,
                        HomeTeamId = homeTeam.Id,
                        AwayTeamId = awayTeam.Id,
                        ScheduledTime = DateTime.UtcNow.AddHours(_random.Next(1, 10)),
                        Status = MatchStatus.Scheduled,
                        HomeScore = 0,
                        AwayScore = 0,
                        Minute = 0,
                        Venue = $"Stadium {i}",
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    matches.Add(match);
                }
                context.Matches.AddRange(matches);


                // --- 5. Final Save ---
                await context.SaveChangesAsync();
            }
        }
    }
}