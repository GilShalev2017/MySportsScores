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

            // Make sure the database exists and migrations are applied
            await context.Database.MigrateAsync();

            if (!await context.Leagues.AnyAsync())
            {
                var league = new League
                {
                    Id = 1,
                    Name = "Premier League",
                    Sport = SportType.Football,
                    Country = "England",
                    Season = 2024,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.Leagues.Add(league);
            }

            if (!await context.Teams.AnyAsync())
            {
                var team1 = new Team
                {
                    Id = 1,
                    Name = "Manchester United",
                    ShortName = "MANU",
                    LogoUrl = "https://example.com/manu-logo.png",
                    Country = "England",
                    LeagueId = 1,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var team2 = new Team
                {
                    Id = 2,
                    Name = "Liverpool FC",
                    ShortName = "LIV",
                    LogoUrl = "https://example.com/liverpool-logo.png",
                    Country = "England",
                    LeagueId = 1,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.Teams.AddRange(team1, team2);
            }

            if (!await context.Players.AnyAsync())
            {
                var player = new Player
                {
                    Id = 1,
                    FirstName = "Mohamed",
                    LastName = "Salah",
                    ShirtNumber = 11,
                    Position = "Forward",
                    TeamId = 2,
                    DateOfBirth = new DateTime(1992, 6, 15),
                    Nationality = "Egypt",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.Players.Add(player);
            }

            if (!await context.Matches.AnyAsync())
            {
                var match = new Match
                {
                    Id = 1,
                    LeagueId = 1,
                    HomeTeamId = 1,
                    AwayTeamId = 2,
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
