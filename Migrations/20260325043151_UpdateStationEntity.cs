using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darb.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateStationEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DurationToEndStation",
                table: "Stations");

            migrationBuilder.DropColumn(
                name: "ExtraFee",
                table: "Stations");

            migrationBuilder.DropColumn(
                name: "Order",
                table: "Stations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "DurationToEndStation",
                table: "Stations",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<decimal>(
                name: "ExtraFee",
                table: "Stations",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "Stations",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
