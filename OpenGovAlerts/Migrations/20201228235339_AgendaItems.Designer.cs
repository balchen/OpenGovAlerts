﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenGovAlerts.Models;

namespace OpenGovAlerts.Migrations
{
    [DbContext(typeof(AlertsDbContext))]
    [Migration("20201228235339_AgendaItems")]
    partial class AgendaItems
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.1.14-servicing-32113")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("OpenGov.Models.AgendaItem", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("ExternalId");

                    b.Property<int?>("MeetingId");

                    b.Property<string>("Number");

                    b.Property<DateTime>("Retrieved");

                    b.Property<string>("Title");

                    b.Property<string>("Url");

                    b.HasKey("Id");

                    b.HasIndex("MeetingId");

                    b.ToTable("AgendaItems");
                });

            modelBuilder.Entity("OpenGov.Models.Document", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int?>("AgendaItemId");

                    b.Property<string>("Text");

                    b.Property<string>("Title");

                    b.Property<string>("Type");

                    b.Property<string>("Url");

                    b.HasKey("Id");

                    b.HasIndex("AgendaItemId");

                    b.ToTable("Documents");
                });

            modelBuilder.Entity("OpenGov.Models.Match", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int?>("AgendaItemId");

                    b.Property<string>("Excerpt");

                    b.Property<int?>("MeetingId");

                    b.Property<int?>("SearchId");

                    b.Property<DateTime>("TimeFound");

                    b.Property<DateTime?>("TimeNotified");

                    b.HasKey("Id");

                    b.HasIndex("AgendaItemId");

                    b.HasIndex("MeetingId");

                    b.HasIndex("SearchId");

                    b.ToTable("Matches");
                });

            modelBuilder.Entity("OpenGov.Models.Meeting", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("BoardId");

                    b.Property<string>("BoardName");

                    b.Property<DateTime>("Date");

                    b.Property<string>("ExternalId");

                    b.Property<int?>("SourceId");

                    b.Property<string>("Url");

                    b.HasKey("Id");

                    b.HasIndex("SourceId");

                    b.ToTable("Meetings");
                });

            modelBuilder.Entity("OpenGov.Models.Observer", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Emails")
                        .HasColumnName("Email");

                    b.Property<string>("Name");

                    b.Property<string>("SmtpPassword");

                    b.Property<int>("SmtpPort");

                    b.Property<string>("SmtpSender");

                    b.Property<string>("SmtpServer");

                    b.Property<bool>("SmtpUseSsl");

                    b.HasKey("Id");

                    b.ToTable("Observers");
                });

            modelBuilder.Entity("OpenGov.Models.ObserverSearch", b =>
                {
                    b.Property<int>("ObserverId");

                    b.Property<int>("SearchId");

                    b.Property<DateTime>("Activated");

                    b.HasKey("ObserverId", "SearchId");

                    b.HasIndex("SearchId");

                    b.ToTable("ObserverSearches");
                });

            modelBuilder.Entity("OpenGov.Models.Search", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int?>("CreatedById");

                    b.Property<string>("Name");

                    b.Property<string>("Phrase");

                    b.Property<DateTime>("Start");

                    b.HasKey("Id");

                    b.HasIndex("CreatedById");

                    b.ToTable("Searches");
                });

            modelBuilder.Entity("OpenGov.Models.SearchSource", b =>
                {
                    b.Property<int>("SearchId");

                    b.Property<int>("SourceId");

                    b.HasKey("SearchId", "SourceId");

                    b.HasIndex("SourceId");

                    b.ToTable("SearchSources");
                });

            modelBuilder.Entity("OpenGov.Models.SeenAgendaItem", b =>
                {
                    b.Property<int>("SearchId");

                    b.Property<int>("AgendaItemId");

                    b.Property<DateTime>("DateSeen");

                    b.HasKey("SearchId", "AgendaItemId");

                    b.HasIndex("AgendaItemId");

                    b.ToTable("SeenAgendaItems");
                });

            modelBuilder.Entity("OpenGov.Models.Source", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Name");

                    b.Property<string>("Url");

                    b.HasKey("Id");

                    b.ToTable("Sources");
                });

            modelBuilder.Entity("OpenGov.Models.StorageConfig", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int?>("ObserverId");

                    b.Property<string>("Url");

                    b.HasKey("Id");

                    b.HasIndex("ObserverId");

                    b.ToTable("StorageConfig");
                });

            modelBuilder.Entity("OpenGov.Models.TaskManagerConfig", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int?>("ObserverId");

                    b.Property<string>("Url");

                    b.HasKey("Id");

                    b.HasIndex("ObserverId");

                    b.ToTable("TaskManagerConfig");
                });

            modelBuilder.Entity("OpenGov.Models.AgendaItem", b =>
                {
                    b.HasOne("OpenGov.Models.Meeting", "Meeting")
                        .WithMany("AgendaItems")
                        .HasForeignKey("MeetingId");
                });

            modelBuilder.Entity("OpenGov.Models.Document", b =>
                {
                    b.HasOne("OpenGov.Models.AgendaItem", "AgendaItem")
                        .WithMany("Documents")
                        .HasForeignKey("AgendaItemId");
                });

            modelBuilder.Entity("OpenGov.Models.Match", b =>
                {
                    b.HasOne("OpenGov.Models.AgendaItem", "AgendaItem")
                        .WithMany("Matches")
                        .HasForeignKey("AgendaItemId");

                    b.HasOne("OpenGov.Models.Meeting")
                        .WithMany("Matches")
                        .HasForeignKey("MeetingId");

                    b.HasOne("OpenGov.Models.Search", "Search")
                        .WithMany("Matches")
                        .HasForeignKey("SearchId");
                });

            modelBuilder.Entity("OpenGov.Models.Meeting", b =>
                {
                    b.HasOne("OpenGov.Models.Source", "Source")
                        .WithMany("Meetings")
                        .HasForeignKey("SourceId");
                });

            modelBuilder.Entity("OpenGov.Models.ObserverSearch", b =>
                {
                    b.HasOne("OpenGov.Models.Observer", "Observer")
                        .WithMany("SubscribedSearches")
                        .HasForeignKey("ObserverId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("OpenGov.Models.Search", "Search")
                        .WithMany("Subscribers")
                        .HasForeignKey("SearchId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("OpenGov.Models.Search", b =>
                {
                    b.HasOne("OpenGov.Models.Observer", "CreatedBy")
                        .WithMany("CreatedSearches")
                        .HasForeignKey("CreatedById");
                });

            modelBuilder.Entity("OpenGov.Models.SearchSource", b =>
                {
                    b.HasOne("OpenGov.Models.Search", "Search")
                        .WithMany("Sources")
                        .HasForeignKey("SearchId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("OpenGov.Models.Source", "Source")
                        .WithMany()
                        .HasForeignKey("SourceId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("OpenGov.Models.SeenAgendaItem", b =>
                {
                    b.HasOne("OpenGov.Models.AgendaItem", "AgendaItem")
                        .WithMany()
                        .HasForeignKey("AgendaItemId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("OpenGov.Models.Search", "Search")
                        .WithMany("SeenAgendaItems")
                        .HasForeignKey("SearchId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("OpenGov.Models.StorageConfig", b =>
                {
                    b.HasOne("OpenGov.Models.Observer")
                        .WithMany("Storage")
                        .HasForeignKey("ObserverId");
                });

            modelBuilder.Entity("OpenGov.Models.TaskManagerConfig", b =>
                {
                    b.HasOne("OpenGov.Models.Observer")
                        .WithMany("TaskManager")
                        .HasForeignKey("ObserverId");
                });
#pragma warning restore 612, 618
        }
    }
}