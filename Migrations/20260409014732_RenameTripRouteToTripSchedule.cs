using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darb.Api.Migrations
{
    /// <inheritdoc />
    public partial class RenameTripRouteToTripSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_TripRoutes_TripRouteId",
                table: "Bookings");

            migrationBuilder.RenameTable(
                name: "TripRoutes",
                newName: "TripSchedules");

            migrationBuilder.RenameColumn(
                name: "TripRouteId",
                table: "TripSchedules",
                newName: "TripScheduleId");

            migrationBuilder.RenameColumn(
                name: "TripRouteId",
                table: "Bookings",
                newName: "TripScheduleId");

            migrationBuilder.RenameIndex(
                name: "IX_Bookings_TripRouteId",
                table: "Bookings",
                newName: "IX_Bookings_TripScheduleId");
            
            migrationBuilder.RenameIndex(
                name: "IX_TripRoutes_StationId",
                table: "TripSchedules",
                newName: "IX_TripSchedules_StationId");

            migrationBuilder.RenameIndex(
                name: "IX_TripRoutes_TripId",
                table: "TripSchedules",
                newName: "IX_TripSchedules_TripId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_TripSchedules_TripScheduleId",
                table: "Bookings",
                column: "TripScheduleId",
                principalTable: "TripSchedules",
                principalColumn: "TripScheduleId",
                onDelete: ReferentialAction.Restrict);
            
             migrationBuilder.AddForeignKey(
                name: "FK_TripSchedules_Stations_StationId",
                table: "TripSchedules",
                column: "StationId",
                principalTable: "Stations",
                principalColumn: "StationId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TripSchedules_Trips_TripId",
                table: "TripSchedules",
                column: "TripId",
                principalTable: "Trips",
                principalColumn: "TripId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_TripSchedules_TripScheduleId",
                table: "Bookings");

            migrationBuilder.DropTable(
                name: "TripSchedules");

            migrationBuilder.RenameColumn(
                name: "TripScheduleId",
                table: "Bookings",
                newName: "TripRouteId");

            migrationBuilder.RenameIndex(
                name: "IX_Bookings_TripScheduleId",
                table: "Bookings",
                newName: "IX_Bookings_TripRouteId");

            migrationBuilder.CreateTable(
                name: "TripRoutes",
                columns: table => new
                {
                    TripRouteId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StationId = table.Column<int>(type: "int", nullable: false),
                    TripId = table.Column<int>(type: "int", nullable: false),
                    DepartureTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    RouteFare = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripRoutes", x => x.TripRouteId);
                    table.ForeignKey(
                        name: "FK_TripRoutes_Stations_StationId",
                        column: x => x.StationId,
                        principalTable: "Stations",
                        principalColumn: "StationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TripRoutes_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "TripId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_TripRoutes_StationId",
                table: "TripRoutes",
                column: "StationId");

            migrationBuilder.CreateIndex(
                name: "IX_TripRoutes_TripId",
                table: "TripRoutes",
                column: "TripId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_TripRoutes_TripRouteId",
                table: "Bookings",
                column: "TripRouteId",
                principalTable: "TripRoutes",
                principalColumn: "TripRouteId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
