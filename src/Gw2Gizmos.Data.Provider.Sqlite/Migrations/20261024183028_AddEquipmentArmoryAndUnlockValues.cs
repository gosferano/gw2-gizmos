using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gw2Gizmos.Data.Provider.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipmentArmoryAndUnlockValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "LegendaryArmoryCopper",
                table: "AccountValueSnapshots",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "UnlocksCopper",
                table: "AccountValueSnapshots",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LegendaryArmoryCopper",
                table: "AccountValueSnapshots");

            migrationBuilder.DropColumn(
                name: "UnlocksCopper",
                table: "AccountValueSnapshots");
        }
    }
}
