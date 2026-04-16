using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darb.Api.Migrations
{
    /// <inheritdoc />
    public partial class addTripFareTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TripFares",
                columns: table => new
                {
                    TripFareId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    FromGovId = table.Column<int>(type: "int", nullable: false),
                    ToGovId = table.Column<int>(type: "int", nullable: false),
                    StationId = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MinutesOffset = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripFares", x => x.TripFareId);
                    table.ForeignKey(
                        name: "FK_TripFares_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "CompanyId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TripFares_Governorates_FromGovId",
                        column: x => x.FromGovId,
                        principalTable: "Governorates",
                        principalColumn: "GovernorateId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TripFares_Governorates_ToGovId",
                        column: x => x.ToGovId,
                        principalTable: "Governorates",
                        principalColumn: "GovernorateId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TripFares_Stations_StationId",
                        column: x => x.StationId,
                        principalTable: "Stations",
                        principalColumn: "StationId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TripFares_CompanyId",
                table: "TripFares",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_TripFares_FromGovId",
                table: "TripFares",
                column: "FromGovId");

            migrationBuilder.CreateIndex(
                name: "IX_TripFares_StationId",
                table: "TripFares",
                column: "StationId");

            migrationBuilder.CreateIndex(
                name: "IX_TripFares_ToGovId",
                table: "TripFares",
                column: "ToGovId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TripFares");
        }
    }
}
