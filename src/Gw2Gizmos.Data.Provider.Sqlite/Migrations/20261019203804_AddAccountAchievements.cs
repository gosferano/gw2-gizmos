using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gw2Gizmos.Data.Provider.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountAchievements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountAchievements",
                columns: table => new
                {
                    AccountId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    AchievementId = table.Column<int>(type: "INTEGER", nullable: false),
                    Current = table.Column<int>(type: "INTEGER", nullable: true),
                    Max = table.Column<int>(type: "INTEGER", nullable: true),
                    Done = table.Column<bool>(type: "INTEGER", nullable: false),
                    Bits = table.Column<string>(type: "TEXT", nullable: false),
                    Repeated = table.Column<int>(type: "INTEGER", nullable: true),
                    Unlocked = table.Column<bool>(type: "INTEGER", nullable: true),
                    SyncedAtUtc = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountAchievements", x => new { x.AccountId, x.AchievementId });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountAchievements");
        }
    }
}
