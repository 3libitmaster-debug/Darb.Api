using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darb.Api.Migrations
{
    /// <inheritdoc />
    public partial class altertableBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_BankAccounts_BankAccountId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_BankAccountId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "PassengerDetails");

            migrationBuilder.DropColumn(
                name: "BankAccountId",
                table: "Bookings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Gender",
                table: "PassengerDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BankAccountId",
                table: "Bookings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_BankAccountId",
                table: "Bookings",
                column: "BankAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_BankAccounts_BankAccountId",
                table: "Bookings",
                column: "BankAccountId",
                principalTable: "BankAccounts",
                principalColumn: "BankAccountId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
