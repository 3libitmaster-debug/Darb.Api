using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darb.Api.Migrations
{
    /// <inheritdoc />
    public partial class ChangeIsActiveProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Advertisements",
                newName: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "Advertisements",
                newName: "Status");
        }
    }
}
