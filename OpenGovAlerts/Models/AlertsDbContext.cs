using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OpenGov.Models;
using System;

namespace OpenGovAlerts.Models
{
    public class AlertsDbContext : DbContext
    {
        public AlertsDbContext(DbContextOptions options)
            : base(options)
        { }

        public DbSet<Document> Documents { get; set; }
        public DbSet<Meeting> Meetings { get; set; }
        public DbSet<Source> Sources { get; set; }
        public DbSet<Observer> Observers { get; set; }
        public DbSet<Search> Searches { get; set; }
        public DbSet<SearchSource> SearchSources { get; set; }
        public DbSet<SeenMeeting> SeenMeetings { get; set; }
        public DbSet<Match> Matches { get; set; }

        public async virtual void UpdateSchema()
        {
            await Database.MigrateAsync();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Source>()
                .HasMany(s => s.Meetings)
                .WithOne(m => m.Source);

            modelBuilder.Entity<Meeting>()
                .HasMany(m => m.Documents)
                .WithOne(d => d.Meeting);

            modelBuilder.Entity<Observer>()
                .HasMany(o => o.Searches)
                .WithOne(s => s.Observer);

            modelBuilder.Entity<Observer>()
                .Property(o => o.Emails)
                .HasColumnName("Email")
                .HasConversion(
                    e => string.Join(',', e),
                    e => e.Split(',', StringSplitOptions.RemoveEmptyEntries));

            modelBuilder.Entity<SearchSource>()
                .HasKey(s => new { s.SearchId, s.SourceId });

            modelBuilder.Entity<SeenMeeting>()
                .HasKey(m => new { m.SearchId, m.MeetingId });

            modelBuilder.Entity<Search>()
                .HasMany(s => s.Sources)
                .WithOne(s => s.Search);

            modelBuilder.Entity<Search>()
                .HasMany(s => s.SeenMeetings)
                .WithOne(m => m.Search);

            modelBuilder.Entity<Search>()
                .HasMany(s => s.Matches)
                .WithOne(m => m.Search);

            modelBuilder.Entity<Search>()
                .HasMany(s => s.Sources)
                .WithOne(s => s.Search);

            modelBuilder.Entity<Meeting>()
                .HasMany(m => m.Matches)
                .WithOne(m => m.Meeting);

            modelBuilder.Entity<Meeting>()
                .Property(m => m.Url)
                .HasConversion(v => v.ToString(), v => new Uri(v));

            modelBuilder.Entity<Meeting>()
                .Ignore(m => m.DocumentsUrl);

            modelBuilder.Entity<Document>()
                .Property(d => d.Url)
                .HasConversion(v => v.ToString(), v => new Uri(v));
        }
    }
}
