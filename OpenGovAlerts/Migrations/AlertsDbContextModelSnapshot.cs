﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenGovAlerts.Models;

namespace OpenGovAlerts.Migrations
{
    [DbContext(typeof(AlertsDbContext))]
    partial class AlertsDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.1.11-servicing-32099")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("OpenGov.Models.Document", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int?>("MeetingId");

                    b.Property<string>("Text");

                    b.Property<string>("Title");

                    b.Property<string>("Type");

                    b.Property<string>("Url");

                    b.HasKey("Id");

                    b.HasIndex("MeetingId");

                    b.ToTable("Documents");
                });

            modelBuilder.Entity("OpenGov.Models.Match", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Excerpt");

                    b.Property<int?>("MeetingId");

                    b.Property<int?>("SearchId");

                    b.Property<DateTime>("TimeFound");

                    b.Property<DateTime?>("TimeNotified");

                    b.HasKey("Id");

                    b.HasIndex("MeetingId");

                    b.HasIndex("SearchId");

                    b.ToTable("Matches");
                });

            modelBuilder.Entity("OpenGov.Models.Meeting", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("AgendaItemId");

                    b.Property<string>("BoardId");

                    b.Property<string>("BoardName");

                    b.Property<DateTime>("Date");

                    b.Property<string>("MeetingId");

                    b.Property<int?>("SourceId");

                    b.Property<string>("Title");

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

                    b.Property<string>("Email");

                    b.Property<string>("Name");

                    b.Property<string>("SmtpPassword");

                    b.Property<int>("SmtpPort");

                    b.Property<string>("SmtpSender");

                    b.Property<string>("SmtpServer");

                    b.Property<bool>("SmtpUseSsl");

                    b.HasKey("Id");

                    b.ToTable("Observers");
                });

            modelBuilder.Entity("OpenGov.Models.Search", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Name");

                    b.Property<int?>("ObserverId");

                    b.Property<string>("Phrase");

                    b.Property<DateTime>("Start");

                    b.HasKey("Id");

                    b.HasIndex("ObserverId");

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

            modelBuilder.Entity("OpenGov.Models.SeenMeeting", b =>
                {
                    b.Property<int>("SearchId");

                    b.Property<int>("MeetingId");

                    b.Property<DateTime>("DateSeen");

                    b.HasKey("SearchId", "MeetingId");

                    b.HasIndex("MeetingId");

                    b.ToTable("SeenMeetings");
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

            modelBuilder.Entity("OpenGov.Models.Document", b =>
                {
                    b.HasOne("OpenGov.Models.Meeting", "Meeting")
                        .WithMany("Documents")
                        .HasForeignKey("MeetingId");
                });

            modelBuilder.Entity("OpenGov.Models.Match", b =>
                {
                    b.HasOne("OpenGov.Models.Meeting", "Meeting")
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

            modelBuilder.Entity("OpenGov.Models.Search", b =>
                {
                    b.HasOne("OpenGov.Models.Observer", "Observer")
                        .WithMany("Searches")
                        .HasForeignKey("ObserverId");
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

            modelBuilder.Entity("OpenGov.Models.SeenMeeting", b =>
                {
                    b.HasOne("OpenGov.Models.Meeting", "Meeting")
                        .WithMany()
                        .HasForeignKey("MeetingId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("OpenGov.Models.Search", "Search")
                        .WithMany("SeenMeetings")
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