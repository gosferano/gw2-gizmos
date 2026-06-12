using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gw2Gizmos.Data.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PriceSnapshots",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    TimestampUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Buy = table.Column<int>(type: "INTEGER", nullable: false),
                    Sell = table.Column<int>(type: "INTEGER", nullable: false),
                    Demand = table.Column<int>(type: "INTEGER", nullable: false),
                    Supply = table.Column<int>(type: "INTEGER", nullable: false),
                    Sold = table.Column<int>(type: "INTEGER", nullable: false),
                    Bought = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceSnapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PriceSnapshots_ItemId_TimestampUtc",
                table: "PriceSnapshots",
                columns: new[] { "ItemId", "TimestampUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PriceSnapshots");
        }
    }
}
