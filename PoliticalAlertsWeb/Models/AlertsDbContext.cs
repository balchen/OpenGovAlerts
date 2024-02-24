﻿using Microsoft.EntityFrameworkCore;
using PoliticalAlerts.Models;
using System;
using System.Threading.Tasks;

namespace PoliticalAlertsWeb.Models
{
    public class AlertsDbContext : DbContext
    {
        public AlertsDbContext(DbContextOptions options)
            : base(options)
        { }

        public DbSet<User> Users { get; set; }
        public DbSet<UserObserver> UserObservers { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<AgendaItem> AgendaItems { get; set; }
        public DbSet<Meeting> Meetings { get; set; }
        public DbSet<Source> Sources { get; set; }
        public DbSet<Observer> Observers { get; set; }
        public DbSet<Search> Searches { get; set; }
        public DbSet<ObserverSearch> ObserverSearches { get; set; }
        public DbSet<SearchSource> SearchSources { get; set; }
        public DbSet<SeenAgendaItem> SeenAgendaItems { get; set; }
        public DbSet<Match> Matches { get; set; }
        public DbSet<JournalEntry> JournalEntries { get; set; }
        public DbSet<ConsultationSearch> ConsultationSearches { get; set; }
        public DbSet<ObserverConsultationSearch> ObserverConsultationSearches { get; set; }
        public DbSet<ConsultationSearchSource> ConsultationSearchSources { get; set; }
        public DbSet<SeenJournalEntry> SeenJournalEntries { get; set; }
        public DbSet<ConsultationMatch> ConsultationMatches { get; set; }

        public async virtual Task UpdateSchema()
        {
            Database.SetCommandTimeout(300000);
            await Database.MigrateAsync();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username, "IX_User_Username");

            modelBuilder.Entity<UserObserver>()
                .HasKey(u => new { u.UserId, u.ObserverId });

            modelBuilder.Entity<Source>()
                .HasMany(s => s.Meetings)
                .WithOne(m => m.Source);

            modelBuilder.Entity<Meeting>()
                .HasMany(m => m.AgendaItems)
                .WithOne(a => a.Meeting);

            modelBuilder.Entity<AgendaItem>()
                .HasMany(m => m.Documents)
                .WithOne(d => d.AgendaItem);

            modelBuilder.Entity<Observer>()
                .HasMany(o => o.CreatedSearches)
                .WithOne(s => s.CreatedBy);

            modelBuilder.Entity<Observer>()
                .HasMany(o => o.SubscribedSearches)
                .WithOne(s => s.Observer);

            modelBuilder.Entity<Search>()
                .HasMany(s => s.Subscribers)
                .WithOne(s => s.Search);

            modelBuilder.Entity<Observer>()
                .Property(o => o.Emails)
                .HasColumnName("Email")
                .HasConversion(
                    e => string.Join(',', e),
                    e => e.Split(',', StringSplitOptions.RemoveEmptyEntries));

            modelBuilder.Entity<ObserverSearch>()
                .HasKey(s => new { s.ObserverId, s.SearchId });

            modelBuilder.Entity<SearchSource>()
                .HasKey(s => new { s.SearchId, s.SourceId });

            modelBuilder.Entity<SeenAgendaItem>()
                .HasKey(m => new { m.SearchId, m.AgendaItemId });

            modelBuilder.Entity<ObserverConsultationSearch>()
                .HasKey(s => new { s.ObserverId, s.ConsultationSearchId });

            modelBuilder.Entity<ConsultationSearchSource>()
                .HasKey(s => new { s.ConsultationSearchId, s.SourceId });

            modelBuilder.Entity<SeenJournalEntry>()
                .HasKey(m => new { m.ConsultationSearchId, m.JournalEntryId });

            modelBuilder.Entity<Search>()
                .HasMany(s => s.Sources)
                .WithOne(s => s.Search);

            modelBuilder.Entity<Search>()
                .HasMany(s => s.SeenAgendaItems)
                .WithOne(m => m.Search);

            modelBuilder.Entity<Search>()
                .HasMany(s => s.Matches)
                .WithOne(m => m.Search);

            modelBuilder.Entity<Search>()
                .HasMany(s => s.Sources)
                .WithOne(s => s.Search);

            modelBuilder.Entity<AgendaItem>()
                .HasMany(a => a.Matches)
                .WithOne(m => m.AgendaItem);

            modelBuilder.Entity<Meeting>()
                .Property(m => m.Url)
                .HasConversion(v => v.ToString(), v => new Uri(v));

            modelBuilder.Entity<AgendaItem>()
                .Property(a => a.Url)
                .HasConversion(v => v.ToString(), v => new Uri(v));

            modelBuilder.Entity<AgendaItem>()
                .Ignore(a => a.DocumentsUrl);

            modelBuilder.Entity<Document>()
                .Property(d => d.Url)
                .HasConversion(v => v.ToString(), v => new Uri(v));

            modelBuilder.Entity<JournalEntry>()
                .HasMany(s => s.Documents)
                .WithOne(d => d.JournalEntry);

            modelBuilder.Entity<JournalEntry>()
                .Property(e => e.Url)
                .HasConversion(v => v.ToString(), v => new Uri(v));
        }
    }
}