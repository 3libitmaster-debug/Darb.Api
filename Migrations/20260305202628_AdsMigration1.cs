using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darb.Api.Migrations
{
    /// <inheritdoc />
    public partial class AdsMigration1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Advertisements_Users_CreatedBy",
                table: "Advertisements");

            migrationBuilder.DropForeignKey(
                name: "FK_Advertisements_Users_UserID",
                table: "Advertisements");

            migrationBuilder.DropIndex(
                name: "IX_Advertisements_CreatedBy",
                table: "Advertisements");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Advertisements");

            migrationBuilder.RenameColumn(
                name: "UserID",
                table: "Advertisements",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "AdvertisementTitle",
                table: "Advertisements",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "AdvertisementStatus",
                table: "Advertisements",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "AdvertisementCreatedAt",
                table: "Advertisements",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_Advertisements_UserID",
                table: "Advertisements",
                newName: "IX_Advertisements_UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Advertisements_Users_UserId",
                table: "Advertisements",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Advertisements_Users_UserId",
                table: "Advertisements");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Advertisements",
                newName: "UserID");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "Advertisements",
                newName: "AdvertisementTitle");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Advertisements",
                newName: "AdvertisementStatus");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Advertisements",
                newName: "AdvertisementCreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_Advertisements_UserId",
                table: "Advertisements",
                newName: "IX_Advertisements_UserID");

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "Advertisements",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Advertisements_CreatedBy",
                table: "Advertisements",
                column: "CreatedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_Advertisements_Users_CreatedBy",
                table: "Advertisements",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Advertisements_Users_UserID",
                table: "Advertisements",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
