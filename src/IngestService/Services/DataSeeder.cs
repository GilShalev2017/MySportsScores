using Microsoft.EntityFrameworkCore;
using Common.Data;
using Common.Models;

namespace IngestService.Seeding
{
    public static class DataSeeder
    {
        public static async Task SeedDatabaseAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<SportsDbContext>();

            // 1. Check and create League
            if (!await context.Leagues.AnyAsync())
            {
                var league = new League
                {
                    // REMOVE: Id = 1,
                    Name = "Premier League",
                    Sport = SportType.Football,
                    Country = "England",
                    Season = 2024,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                context.Leagues.Add(league);
                // League is now tracked, but ID is not yet assigned.

                // 2. Check and create Teams
                if (!await context.Teams.AnyAsync())
                {
                    var team1 = new Team
                    {
                        // REMOVE: Id = 1,
                        Name = "Manchester United",
                        ShortName = "MANU",
                        LogoUrl = "https://example.com/manu-logo.png",
                        Country = "England",
                        // ASSIGN BY REFERENCE, NOT HARDCODED ID
                        LeagueId = league.Id, // EF Core will resolve this upon save.
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    var team2 = new Team
                    {
                        // REMOVE: Id = 2,
                        Name = "Liverpool FC",
                        ShortName = "LIV",
                        LogoUrl = "https://example.com/liverpool-logo.png",
                        Country = "England",
                        // ASSIGN BY REFERENCE, NOT HARDCODED ID
                        LeagueId = league.Id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    context.Teams.AddRange(team1, team2);

                    // 3. Check and create Players
                    if (!await context.Players.AnyAsync())
                    {
                        var player = new Player
                        {
                            // REMOVE: Id = 1,
                            FirstName = "Mohamed",
                            LastName = "Salah",
                            ShirtNumber = 11,
                            Position = "Forward",
                            // ASSIGN BY REFERENCE, NOT HARDCODED ID
                            TeamId = team2.Id,
                            DateOfBirth = new DateTime(1992, 6, 15),
                            Nationality = "Egypt",
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        context.Players.Add(player);
                    }

                    // 4. Check and create Matches
                    if (!await context.Matches.AnyAsync())
                    {
                        var match = new Match
                        {
                            // REMOVE: Id = 1,
                            // ASSIGN BY REFERENCE
                            LeagueId = league.Id,
                            HomeTeamId = team1.Id,
                            AwayTeamId = team2.Id,
                            ScheduledTime = DateTime.UtcNow.AddHours(5),
                            Status = MatchStatus.Scheduled,
                            HomeScore = 0,
                            AwayScore = 0,
                            Minute = 0,
                            Venue = "Old Trafford",
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        context.Matches.Add(match);
                    }

                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
