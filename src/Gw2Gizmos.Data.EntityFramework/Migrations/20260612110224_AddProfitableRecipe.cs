using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gw2Gizmos.Data.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddProfitableRecipe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProfitableRecipes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OutputItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    OutputItemName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    CraftingCost = table.Column<double>(type: "REAL", nullable: false),
                    SellPrice = table.Column<int>(type: "INTEGER", nullable: false),
                    BuyPrice = table.Column<int>(type: "INTEGER", nullable: false),
                    Profit = table.Column<double>(type: "REAL", nullable: false),
                    MarginPercent = table.Column<double>(type: "REAL", nullable: false),
                    ComputedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    TreeJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfitableRecipes", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProfitableRecipes");
        }
    }
}
