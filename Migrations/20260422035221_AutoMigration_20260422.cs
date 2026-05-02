using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darb.Api.Migrations
{
    /// <inheritdoc />
    public partial class AutoMigration_20260422 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Advertisements_Users_UserId",
                table: "Advertisements");

            migrationBuilder.DropForeignKey(
                name: "FK_Companies_Users_UserId",
                table: "Companies");

            migrationBuilder.DropForeignKey(
                name: "FK_Passengers_Users_UserId",
                table: "Passengers");

            migrationBuilder.DropTable(
                name: "Subscriptions");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Advertisements_UserId",
                table: "Advertisements");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Advertisements");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Trips",
                newName: "TripStatus");

            migrationBuilder.RenameColumn(
                name: "DepartureDate",
                table: "Trips",
                newName: "DepDate");

            migrationBuilder.RenameColumn(
                name: "BasePrice",
                table: "Trips",
                newName: "Price");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Passengers",
                newName: "AccountId");

            migrationBuilder.RenameIndex(
                name: "IX_Passengers_UserId",
                table: "Passengers",
                newName: "IX_Passengers_AccountId");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Companies",
                newName: "AccountId");

            migrationBuilder.RenameIndex(
                name: "IX_Companies_UserId",
                table: "Companies",
                newName: "IX_Companies_AccountId");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Buses",
                newName: "BusStatus");

            migrationBuilder.RenameColumn(
                name: "Capacity",
                table: "Buses",
                newName: "BusCapacity");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Advertisements",
                newName: "AdsStatus");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "Advertisements",
                newName: "AdsTitle");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Advertisements",
                newName: "AdsCreatedAt");

            migrationBuilder.AddColumn<int>(
                name: "AccountId",
                table: "Advertisements",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    AccountId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Password = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    JoinDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.AccountId);
                });

            migrationBuilder.CreateTable(
                name: "CompanySubscription",
                columns: table => new
                {
                    CompanySubscriptionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlanType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubscriptionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaymentSlip = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanySubscription", x => x.CompanySubscriptionId);
                    table.ForeignKey(
                        name: "FK_CompanySubscription_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "CompanyId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Advertisements_AccountId",
                table: "Advertisements",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Email",
                table: "Accounts",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CompanySubscription_CompanyId",
                table: "CompanySubscription",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Advertisements_Accounts_AccountId",
                table: "Advertisements",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "AccountId",
                onDelete: ReferentialAction.Restrict);

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Advertisements_Accounts_AccountId",
                table: "Advertisements");

            migrationBuilder.DropForeignKey(
                name: "FK_Companies_Accounts_AccountId",
                table: "Companies");

            migrationBuilder.DropForeignKey(
                name: "FK_Passengers_Accounts_AccountId",
                table: "Passengers");

            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "CompanySubscription");

            migrationBuilder.DropIndex(
                name: "IX_Advertisements_AccountId",
                table: "Advertisements");

            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "Advertisements");

            migrationBuilder.RenameColumn(
                name: "TripStatus",
                table: "Trips",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "Trips",
                newName: "BasePrice");

            migrationBuilder.RenameColumn(
                name: "DepDate",
                table: "Trips",
                newName: "DepartureDate");

            migrationBuilder.RenameColumn(
                name: "AccountId",
                table: "Passengers",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Passengers_AccountId",
                table: "Passengers",
                newName: "IX_Passengers_UserId");

            migrationBuilder.RenameColumn(
                name: "AccountId",
                table: "Companies",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Companies_AccountId",
                table: "Companies",
                newName: "IX_Companies_UserId");

            migrationBuilder.RenameColumn(
                name: "BusStatus",
                table: "Buses",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "BusCapacity",
                table: "Buses",
                newName: "Capacity");

            migrationBuilder.RenameColumn(
                name: "AdsTitle",
                table: "Advertisements",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "AdsStatus",
                table: "Advertisements",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "AdsCreatedAt",
                table: "Advertisements",
                newName: "CreatedAt");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Advertisements",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    SubscriptionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaymentSlip = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlanType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubscriptionDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.SubscriptionId);
                    table.ForeignKey(
                        name: "FK_Subscriptions_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "CompanyId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    JoinDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Advertisements_UserId",
                table: "Advertisements",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_CompanyId",
                table: "Subscriptions",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Advertisements_Users_UserId",
                table: "Advertisements",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Companies_Users_UserId",
                table: "Companies",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Passengers_Users_UserId",
                table: "Passengers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
