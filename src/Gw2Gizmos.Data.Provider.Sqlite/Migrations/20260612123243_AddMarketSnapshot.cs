using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gw2Gizmos.Data.Provider.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MarketItems",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Buy = table.Column<int>(type: "INTEGER", nullable: false),
                    Demand = table.Column<int>(type: "INTEGER", nullable: false),
                    Sell = table.Column<int>(type: "INTEGER", nullable: false),
                    Supply = table.Column<int>(type: "INTEGER", nullable: false),
                    IsCraftable = table.Column<bool>(type: "INTEGER", nullable: false),
                    CraftingCost = table.Column<double>(type: "REAL", nullable: true),
                    Profit = table.Column<double>(type: "REAL", nullable: true),
                    MarginPercent = table.Column<double>(type: "REAL", nullable: true),
                    ComputedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketItems", x => x.ItemId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarketItems");
        }
    }
}
