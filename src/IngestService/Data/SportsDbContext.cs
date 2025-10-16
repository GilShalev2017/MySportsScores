using Common.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace IngestService.Data
{
    public class SportsDbContext : DbContext
    {
        public SportsDbContext(DbContextOptions<SportsDbContext> options) : base(options) { }

        public DbSet<League> Leagues { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<Player> Players { get; set; }
        public DbSet<Match> Matches { get; set; }
        public DbSet<UserPreference> UserPreferences { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<League>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.HasIndex(e => new { e.Sport, e.Country });
            });

            modelBuilder.Entity<Team>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.HasIndex(e => e.LeagueId);
            });

            modelBuilder.Entity<Player>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.TeamId);
            });

            modelBuilder.Entity<Match>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.Status, e.ScheduledTime });
                entity.HasIndex(e => e.LeagueId);
                entity.HasIndex(e => new { e.HomeTeamId, e.AwayTeamId });
            });

            modelBuilder.Entity<UserPreference>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.UserId).IsUnique();
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
