using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gw2Gizmos.Data.Provider.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountValueSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountValueSnapshots",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AccountId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    TakenAtUtc = table.Column<long>(type: "INTEGER", nullable: false),
                    CoinCopper = table.Column<long>(type: "INTEGER", nullable: false),
                    MaterialsCopper = table.Column<long>(type: "INTEGER", nullable: false),
                    BankCopper = table.Column<long>(type: "INTEGER", nullable: false),
                    SharedInventoryCopper = table.Column<long>(type: "INTEGER", nullable: false),
                    CharactersCopper = table.Column<long>(type: "INTEGER", nullable: false),
                    TradingPostCopper = table.Column<long>(type: "INTEGER", nullable: false),
                    IsBackfilled = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountValueSnapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountValueSnapshots_AccountId_TakenAtUtc",
                table: "AccountValueSnapshots",
                columns: new[] { "AccountId", "TakenAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountValueSnapshots");
        }
    }
}
