using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PoliticalAlertsWeb.Migrations
{
    public partial class CaseIntegration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "JournalEntryId",
                table: "Documents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CaseNumber",
                table: "AgendaItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MonitorConsultations",
                table: "AgendaItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ConsultationSearches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    Start = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Phrase = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsultationSearches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsultationSearches_Observers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Observers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JournalEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExternalId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ParsedType = table.Column<int>(type: "int", nullable: false),
                    AgendaItemId = table.Column<int>(type: "int", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    From = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    To = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JournalEntries_AgendaItems_AgendaItemId",
                        column: x => x.AgendaItemId,
                        principalTable: "AgendaItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConsultationSearchSources",
                columns: table => new
                {
                    ConsultationSearchId = table.Column<int>(type: "int", nullable: false),
                    SourceId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsultationSearchSources", x => new { x.ConsultationSearchId, x.SourceId });
                    table.ForeignKey(
                        name: "FK_ConsultationSearchSources_ConsultationSearches_ConsultationSearchId",
                        column: x => x.ConsultationSearchId,
                        principalTable: "ConsultationSearches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConsultationSearchSources_Sources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "Sources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ObserverConsultationSearches",
                columns: table => new
                {
                    ObserverId = table.Column<int>(type: "int", nullable: false),
                    ConsultationSearchId = table.Column<int>(type: "int", nullable: false),
                    Activated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObserverConsultationSearches", x => new { x.ObserverId, x.ConsultationSearchId });
                    table.ForeignKey(
                        name: "FK_ObserverConsultationSearches_ConsultationSearches_ConsultationSearchId",
                        column: x => x.ConsultationSearchId,
                        principalTable: "ConsultationSearches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ObserverConsultationSearches_Observers_ObserverId",
                        column: x => x.ObserverId,
                        principalTable: "Observers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConsultationMatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SearchId = table.Column<int>(type: "int", nullable: true),
                    JournalEntryId = table.Column<int>(type: "int", nullable: true),
                    TimeFound = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TimeNotified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Excerpt = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsultationMatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsultationMatches_ConsultationSearches_SearchId",
                        column: x => x.SearchId,
                        principalTable: "ConsultationSearches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConsultationMatches_JournalEntries_JournalEntryId",
                        column: x => x.JournalEntryId,
                        principalTable: "JournalEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SeenJournalEntries",
                columns: table => new
                {
                    ConsultationSearchId = table.Column<int>(type: "int", nullable: false),
                    JournalEntryId = table.Column<int>(type: "int", nullable: false),
                    DateSeen = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeenJournalEntries", x => new { x.ConsultationSearchId, x.JournalEntryId });
                    table.ForeignKey(
                        name: "FK_SeenJournalEntries_ConsultationSearches_ConsultationSearchId",
                        column: x => x.ConsultationSearchId,
                        principalTable: "ConsultationSearches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SeenJournalEntries_JournalEntries_JournalEntryId",
                        column: x => x.JournalEntryId,
                        principalTable: "JournalEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_JournalEntryId",
                table: "Documents",
                column: "JournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultationMatches_JournalEntryId",
                table: "ConsultationMatches",
                column: "JournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultationMatches_SearchId",
                table: "ConsultationMatches",
                column: "SearchId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultationSearches_CreatedById",
                table: "ConsultationSearches",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultationSearchSources_SourceId",
                table: "ConsultationSearchSources",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_AgendaItemId",
                table: "JournalEntries",
                column: "AgendaItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ObserverConsultationSearches_ConsultationSearchId",
                table: "ObserverConsultationSearches",
                column: "ConsultationSearchId");

            migrationBuilder.CreateIndex(
                name: "IX_SeenJournalEntries_JournalEntryId",
                table: "SeenJournalEntries",
                column: "JournalEntryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_JournalEntries_JournalEntryId",
                table: "Documents",
                column: "JournalEntryId",
                principalTable: "JournalEntries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_JournalEntries_JournalEntryId",
                table: "Documents");

            migrationBuilder.DropTable(
                name: "ConsultationMatches");

            migrationBuilder.DropTable(
                name: "ConsultationSearchSources");

            migrationBuilder.DropTable(
                name: "ObserverConsultationSearches");

            migrationBuilder.DropTable(
                name: "SeenJournalEntries");

            migrationBuilder.DropTable(
                name: "ConsultationSearches");

            migrationBuilder.DropTable(
                name: "JournalEntries");

            migrationBuilder.DropIndex(
                name: "IX_Documents_JournalEntryId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "JournalEntryId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "CaseNumber",
                table: "AgendaItems");

            migrationBuilder.DropColumn(
                name: "MonitorConsultations",
                table: "AgendaItems");
        }
    }
}
