using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darb.Api.Migrations
{
    /// <inheritdoc />
    public partial class RenameAccountAndPassengerEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Drop old Foreign Keys to allow renaming
            migrationBuilder.DropForeignKey(
                name: "FK_Advertisements_Accounts_AccountId",
                table: "Advertisements");

            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Passengers_PassengerId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_Companies_Accounts_AccountId",
                table: "Companies");

            migrationBuilder.DropForeignKey(
                name: "FK_Passengers_Accounts_AccountId",
                table: "Passengers");

            migrationBuilder.DropForeignKey(
                name: "FK_PassengerDetails_Bookings_BookingId",
                table: "PassengerDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_Review_Passengers_PassengerId",
                table: "Review");

            // 2. Drop old Primary Keys to allow renaming columns
            migrationBuilder.DropPrimaryKey(
                name: "PK_Accounts",
                table: "Accounts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Passengers",
                table: "Passengers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PassengerDetails",
                table: "PassengerDetails");

            // 3. Rename Tables
            migrationBuilder.RenameTable(
                name: "Accounts",
                newName: "Users");

            migrationBuilder.RenameTable(
                name: "Passengers",
                newName: "Customers");

            migrationBuilder.RenameTable(
                name: "PassengerDetails",
                newName: "Passenger");

            // 4. Rename Columns in renamed tables
            migrationBuilder.RenameColumn(
                name: "AccountId",
                table: "Users",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "PassengerId",
                table: "Customers",
                newName: "CustomerId");

            migrationBuilder.RenameColumn(
                name: "AccountId",
                table: "Customers",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "PassengerDetailsId",
                table: "Passenger",
                newName: "PassengerId");

            // 5. Rename Columns in referencing tables
            migrationBuilder.RenameColumn(
                name: "AccountId",
                table: "Advertisements",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "AccountId",
                table: "Companies",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "PassengerId",
                table: "Bookings",
                newName: "CustomerId");

            migrationBuilder.RenameColumn(
                name: "PassengerId",
                table: "Review",
                newName: "CustomerId");

            // 6. Rename Indexes
            migrationBuilder.RenameIndex(
                name: "IX_Accounts_Email",
                table: "Users",
                newName: "IX_Users_Email");

            migrationBuilder.RenameIndex(
                name: "IX_Passengers_Phone",
                table: "Customers",
                newName: "IX_Customers_Phone");

            migrationBuilder.RenameIndex(
                name: "IX_Passengers_AccountId",
                table: "Customers",
                newName: "IX_Customers_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_PassengerDetails_BookingId",
                table: "Passenger",
                newName: "IX_Passenger_BookingId");

            migrationBuilder.RenameIndex(
                name: "IX_Advertisements_AccountId",
                table: "Advertisements",
                newName: "IX_Advertisements_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Companies_AccountId",
                table: "Companies",
                newName: "IX_Companies_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Bookings_PassengerId",
                table: "Bookings",
                newName: "IX_Bookings_CustomerId");

            migrationBuilder.RenameIndex(
                name: "IX_Review_PassengerId",
                table: "Review",
                newName: "IX_Review_CustomerId");

            // 7. Add Primary Keys
            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Customers",
                table: "Customers",
                column: "CustomerId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Passenger",
                table: "Passenger",
                column: "PassengerId");

            // 8. Add Foreign Keys
            migrationBuilder.AddForeignKey(
                name: "FK_Advertisements_Users_UserId",
                table: "Advertisements",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Customers_CustomerId",
                table: "Bookings",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Companies_Users_UserId",
                table: "Companies",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_Users_UserId",
                table: "Customers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Passenger_Bookings_BookingId",
                table: "Passenger",
                column: "BookingId",
                principalTable: "Bookings",
                principalColumn: "BookingId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Review_Customers_CustomerId",
                table: "Review",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "CustomerId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 1. Drop new Foreign Keys
            migrationBuilder.DropForeignKey(
                name: "FK_Advertisements_Users_UserId",
                table: "Advertisements");

            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Customers_CustomerId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_Companies_Users_UserId",
                table: "Companies");

            migrationBuilder.DropForeignKey(
                name: "FK_Customers_Users_UserId",
                table: "Customers");

            migrationBuilder.DropForeignKey(
                name: "FK_Passenger_Bookings_BookingId",
                table: "Passenger");

            migrationBuilder.DropForeignKey(
                name: "FK_Review_Customers_CustomerId",
                table: "Review");

            // 2. Drop new Primary Keys
            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Customers",
                table: "Customers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Passenger",
                table: "Passenger");

            // 3. Rename Tables back
            migrationBuilder.RenameTable(
                name: "Users",
                newName: "Accounts");

            migrationBuilder.RenameTable(
                name: "Customers",
                newName: "Passengers");

            migrationBuilder.RenameTable(
                name: "Passenger",
                newName: "PassengerDetails");

            // 4. Rename Columns back
            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Accounts",
                newName: "AccountId");

            migrationBuilder.RenameColumn(
                name: "CustomerId",
                table: "Passengers",
                newName: "PassengerId");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Passengers",
                newName: "AccountId");

            migrationBuilder.RenameColumn(
                name: "PassengerId",
                table: "PassengerDetails",
                newName: "PassengerDetailsId");

            // 5. Rename Referencing Columns back
            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Advertisements",
                newName: "AccountId");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Companies",
                newName: "AccountId");

            migrationBuilder.RenameColumn(
                name: "CustomerId",
                table: "Bookings",
                newName: "PassengerId");

            migrationBuilder.RenameColumn(
                name: "CustomerId",
                table: "Review",
                newName: "PassengerId");

            // 6. Rename Indexes back
            migrationBuilder.RenameIndex(
                name: "IX_Users_Email",
                table: "Accounts",
                newName: "IX_Accounts_Email");

            migrationBuilder.RenameIndex(
                name: "IX_Customers_Phone",
                table: "Passengers",
                newName: "IX_Passengers_Phone");

            migrationBuilder.RenameIndex(
                name: "IX_Customers_UserId",
                table: "Passengers",
                newName: "IX_Passengers_AccountId");

            migrationBuilder.RenameIndex(
                name: "IX_Passenger_BookingId",
                table: "PassengerDetails",
                newName: "IX_PassengerDetails_BookingId");

            migrationBuilder.RenameIndex(
                name: "IX_Advertisements_UserId",
                table: "Advertisements",
                newName: "IX_Advertisements_AccountId");

            migrationBuilder.RenameIndex(
                name: "IX_Companies_UserId",
                table: "Companies",
                newName: "IX_Companies_AccountId");

            migrationBuilder.RenameIndex(
                name: "IX_Bookings_CustomerId",
                table: "Bookings",
                newName: "IX_Bookings_PassengerId");

            migrationBuilder.RenameIndex(
                name: "IX_Review_CustomerId",
                table: "Review",
                newName: "IX_Review_PassengerId");

            // 7. Re-add old Primary Keys
            migrationBuilder.AddPrimaryKey(
                name: "PK_Accounts",
                table: "Accounts",
                column: "AccountId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Passengers",
                table: "Passengers",
                column: "PassengerId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PassengerDetails",
                table: "PassengerDetails",
                column: "PassengerDetailsId");

            // 8. Re-add old Foreign Keys
            migrationBuilder.AddForeignKey(
                name: "FK_Advertisements_Accounts_AccountId",
                table: "Advertisements",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "AccountId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Passengers_PassengerId",
                table: "Bookings",
                column: "PassengerId",
                principalTable: "Passengers",
                principalColumn: "PassengerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Companies_Accounts_AccountId",
                table: "Companies",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "AccountId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Passengers_Accounts_AccountId",
                table: "Passengers",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "AccountId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PassengerDetails_Bookings_BookingId",
                table: "PassengerDetails",
                column: "BookingId",
                principalTable: "Bookings",
                principalColumn: "BookingId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Review_Passengers_PassengerId",
                table: "Review",
                column: "PassengerId",
                principalTable: "Passengers",
                principalColumn: "PassengerId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
