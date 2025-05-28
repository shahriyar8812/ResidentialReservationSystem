using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SetCheckInRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CheckInRules_UnitId",
                table: "CheckInRules");

            migrationBuilder.CreateIndex(
                name: "IX_CheckInRules_UnitId",
                table: "CheckInRules",
                column: "UnitId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CheckInRules_UnitId",
                table: "CheckInRules");

            migrationBuilder.CreateIndex(
                name: "IX_CheckInRules_UnitId",
                table: "CheckInRules",
                column: "UnitId");
        }
    }
}
