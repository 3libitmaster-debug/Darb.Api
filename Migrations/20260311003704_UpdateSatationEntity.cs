using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darb.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSatationEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TripFares");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Stations");

            migrationBuilder.RenameColumn(
                name: "DurationFromStart",
                table: "Stations",
                newName: "DurationToEndStation");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DurationToEndStation",
                table: "Stations",
                newName: "DurationFromStart");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Stations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "TripFares",
                columns: table => new
                {
                    TripFareId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StationId = table.Column<int>(type: "int", nullable: false),
                    TripId = table.Column<int>(type: "int", nullable: false),
                    ActualDepartureTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    ActualPrice = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripFares", x => x.TripFareId);
                    table.ForeignKey(
                        name: "FK_TripFares_Stations_StationId",
                        column: x => x.StationId,
                        principalTable: "Stations",
                        principalColumn: "StationId");
                    table.ForeignKey(
                        name: "FK_TripFares_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "TripId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TripFares_StationId",
                table: "TripFares",
                column: "StationId");

            migrationBuilder.CreateIndex(
                name: "IX_TripFares_TripId",
                table: "TripFares",
                column: "TripId");
        }
    }
}
