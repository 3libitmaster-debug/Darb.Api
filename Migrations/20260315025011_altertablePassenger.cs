using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darb.Api.Migrations
{
    /// <inheritdoc />
    public partial class altertablePassenger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NationalId",
                table: "Passengers",
                type: "nvarchar(11)",
                maxLength: 11,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsOwnerPassenger",
                table: "Bookings",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NationalId",
                table: "Passengers");

            migrationBuilder.DropColumn(
                name: "IsOwnerPassenger",
                table: "Bookings");
        }
    }
}
