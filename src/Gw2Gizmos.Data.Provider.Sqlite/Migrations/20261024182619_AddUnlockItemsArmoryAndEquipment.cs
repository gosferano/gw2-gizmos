using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gw2Gizmos.Data.Provider.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddUnlockItemsArmoryAndEquipment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountLegendaryArmory",
                columns: table => new
                {
                    AccountId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Count = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountLegendaryArmory", x => new { x.AccountId, x.ItemId });
                });

            migrationBuilder.CreateTable(
                name: "CharacterEquippedItems",
                columns: table => new
                {
                    CharacterName = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Index = table.Column<int>(type: "INTEGER", nullable: false),
                    AccountId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Part = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    Location = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterEquippedItems", x => new { x.CharacterName, x.Index });
                });

            migrationBuilder.CreateTable(
                name: "UnlockItems",
                columns: table => new
                {
                    Kind = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    UnlockId = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnlockItems", x => new { x.Kind, x.UnlockId, x.ItemId });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountLegendaryArmory");

            migrationBuilder.DropTable(
                name: "CharacterEquippedItems");

            migrationBuilder.DropTable(
                name: "UnlockItems");
        }
    }
}
