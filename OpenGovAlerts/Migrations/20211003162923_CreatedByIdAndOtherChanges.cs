using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace OpenGovAlerts.Migrations
{
    public partial class CreatedByIdAndOtherChanges : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Searches_Observers_ObserverId",
                table: "Searches");

            migrationBuilder.RenameColumn(
                name: "ObserverId",
                table: "Searches",
                newName: "CreatedById");

            migrationBuilder.RenameIndex(
                name: "IX_Searches_ObserverId",
                table: "Searches",
                newName: "IX_Searches_CreatedById");

            migrationBuilder.CreateTable(
                name: "ObserverSearches",
                columns: table => new
                {
                    ObserverId = table.Column<int>(type: "int", nullable: false),
                    SearchId = table.Column<int>(type: "int", nullable: false),
                    Activated = table.Column<DateTime>(type: "datetime2", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_ObserverSearches_SearchId",
                table: "ObserverSearches",
                column: "SearchId");

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
                name: "FK_Searches_Observers_CreatedById",
                table: "Searches");

            migrationBuilder.DropTable(
                name: "ObserverSearches");

            migrationBuilder.RenameColumn(
                name: "CreatedById",
                table: "Searches",
                newName: "ObserverId");

            migrationBuilder.RenameIndex(
                name: "IX_Searches_CreatedById",
                table: "Searches",
                newName: "IX_Searches_ObserverId");

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
