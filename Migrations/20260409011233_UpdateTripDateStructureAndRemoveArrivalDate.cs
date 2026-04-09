using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darb.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTripDateStructureAndRemoveArrivalDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArrivalDateTime",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "DepartureDateTime",
                table: "Trips");

            migrationBuilder.AddColumn<DateTime>(
                name: "DepartureDate",
                table: "Trips",
                type: "date",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DepartureDate",
                table: "Trips");

            migrationBuilder.AddColumn<DateTime>(
                name: "ArrivalDateTime",
                table: "Trips",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DepartureDateTime",
                table: "Trips",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
