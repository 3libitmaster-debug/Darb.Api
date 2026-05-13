using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darb.Api.Migrations
{
    /// <inheritdoc />
    public partial class refactorTicketBookingRealtionship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ETickets_PassengerDetails_PassengerDetailId",
                table: "ETickets");

            migrationBuilder.RenameColumn(
                name: "PassengerDetailId",
                table: "ETickets",
                newName: "BookingId");

            migrationBuilder.RenameIndex(
                name: "IX_ETickets_PassengerDetailId",
                table: "ETickets",
                newName: "IX_ETickets_BookingId");

            migrationBuilder.AddForeignKey(
                name: "FK_ETickets_Bookings_BookingId",
                table: "ETickets",
                column: "BookingId",
                principalTable: "Bookings",
                principalColumn: "BookingId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ETickets_Bookings_BookingId",
                table: "ETickets");

            migrationBuilder.RenameColumn(
                name: "BookingId",
                table: "ETickets",
                newName: "PassengerDetailId");

            migrationBuilder.RenameIndex(
                name: "IX_ETickets_BookingId",
                table: "ETickets",
                newName: "IX_ETickets_PassengerDetailId");

            migrationBuilder.AddForeignKey(
                name: "FK_ETickets_PassengerDetails_PassengerDetailId",
                table: "ETickets",
                column: "PassengerDetailId",
                principalTable: "PassengerDetails",
                principalColumn: "PassengerDetailsId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
