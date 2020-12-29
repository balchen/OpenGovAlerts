using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace OpenGovAlerts.Migrations
{
    public partial class AgendaItems : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Meetings_MeetingId",
                table: "Documents");

            migrationBuilder.DropForeignKey(
                name: "FK_Searches_Observers_ObserverId",
                table: "Searches");

            migrationBuilder.DropTable(
                name: "SeenMeetings");

            migrationBuilder.DropColumn(
                name: "AgendaItemId",
                table: "Meetings");

            migrationBuilder.DropColumn(
                name: "MeetingId",
                table: "Meetings");

            migrationBuilder.RenameColumn(
                name: "ObserverId",
                table: "Searches",
                newName: "CreatedById");

            migrationBuilder.RenameIndex(
                name: "IX_Searches_ObserverId",
                table: "Searches",
                newName: "IX_Searches_CreatedById");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "Meetings",
                newName: "ExternalId");

            migrationBuilder.RenameColumn(
                name: "MeetingId",
                table: "Documents",
                newName: "AgendaItemId");

            migrationBuilder.RenameIndex(
                name: "IX_Documents_MeetingId",
                table: "Documents",
                newName: "IX_Documents_AgendaItemId");

            migrationBuilder.AddColumn<int>(
                name: "AgendaItemId",
                table: "Matches",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AgendaItems",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Retrieved = table.Column<DateTime>(nullable: false),
                    MeetingId = table.Column<int>(nullable: true),
                    ExternalId = table.Column<string>(nullable: true),
                    Number = table.Column<string>(nullable: true),
                    Title = table.Column<string>(nullable: true),
                    Url = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgendaItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgendaItems_Meetings_MeetingId",
                        column: x => x.MeetingId,
                        principalTable: "Meetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ObserverSearches",
                columns: table => new
                {
                    ObserverId = table.Column<int>(nullable: false),
                    SearchId = table.Column<int>(nullable: false),
                    Activated = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObserverSearches", x => new { x.ObserverId, x.SearchId });
                    table.ForeignKey(
                        name: "FK_ObserverSearches_Observers_ObserverId",
                        column: x => x.ObserverId,
                        principalTable: "Observers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ObserverSearches_Searches_SearchId",
                        column: x => x.SearchId,
                        principalTable: "Searches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SeenAgendaItems",
                columns: table => new
                {
                    SearchId = table.Column<int>(nullable: false),
                    AgendaItemId = table.Column<int>(nullable: false),
                    DateSeen = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeenAgendaItems", x => new { x.SearchId, x.AgendaItemId });
                    table.ForeignKey(
                        name: "FK_SeenAgendaItems_AgendaItems_AgendaItemId",
                        column: x => x.AgendaItemId,
                        principalTable: "AgendaItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SeenAgendaItems_Searches_SearchId",
                        column: x => x.SearchId,
                        principalTable: "Searches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Matches_AgendaItemId",
                table: "Matches",
                column: "AgendaItemId");

            migrationBuilder.CreateIndex(
                name: "IX_AgendaItems_MeetingId",
                table: "AgendaItems",
                column: "MeetingId");

            migrationBuilder.CreateIndex(
                name: "IX_ObserverSearches_SearchId",
                table: "ObserverSearches",
                column: "SearchId");

            migrationBuilder.CreateIndex(
                name: "IX_SeenAgendaItems_AgendaItemId",
                table: "SeenAgendaItems",
                column: "AgendaItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_AgendaItems_AgendaItemId",
                table: "Documents",
                column: "AgendaItemId",
                principalTable: "AgendaItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_AgendaItems_AgendaItemId",
                table: "Matches",
                column: "AgendaItemId",
                principalTable: "AgendaItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Searches_Observers_CreatedById",
                table: "Searches",
                column: "CreatedById",
                principalTable: "Observers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_AgendaItems_AgendaItemId",
                table: "Documents");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_AgendaItems_AgendaItemId",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Searches_Observers_CreatedById",
                table: "Searches");

            migrationBuilder.DropTable(
                name: "ObserverSearches");

            migrationBuilder.DropTable(
                name: "SeenAgendaItems");

            migrationBuilder.DropTable(
                name: "AgendaItems");

            migrationBuilder.DropIndex(
                name: "IX_Matches_AgendaItemId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "AgendaItemId",
                table: "Matches");

            migrationBuilder.RenameColumn(
                name: "CreatedById",
                table: "Searches",
                newName: "ObserverId");

            migrationBuilder.RenameIndex(
                name: "IX_Searches_CreatedById",
                table: "Searches",
                newName: "IX_Searches_ObserverId");

            migrationBuilder.RenameColumn(
                name: "ExternalId",
                table: "Meetings",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "AgendaItemId",
                table: "Documents",
                newName: "MeetingId");

            migrationBuilder.RenameIndex(
                name: "IX_Documents_AgendaItemId",
                table: "Documents",
                newName: "IX_Documents_MeetingId");

            migrationBuilder.AddColumn<string>(
                name: "AgendaItemId",
                table: "Meetings",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MeetingId",
                table: "Meetings",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SeenMeetings",
                columns: table => new
                {
                    SearchId = table.Column<int>(nullable: false),
                    MeetingId = table.Column<int>(nullable: false),
                    DateSeen = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeenMeetings", x => new { x.SearchId, x.MeetingId });
                    table.ForeignKey(
                        name: "FK_SeenMeetings_Meetings_MeetingId",
                        column: x => x.MeetingId,
                        principalTable: "Meetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SeenMeetings_Searches_SearchId",
                        column: x => x.SearchId,
                        principalTable: "Searches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SeenMeetings_MeetingId",
                table: "SeenMeetings",
                column: "MeetingId");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Meetings_MeetingId",
                table: "Documents",
                column: "MeetingId",
                principalTable: "Meetings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Searches_Observers_ObserverId",
                table: "Searches",
                column: "ObserverId",
                principalTable: "Observers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
