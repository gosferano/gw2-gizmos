using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gw2Gizmos.Data.Provider.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddResetProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountResetActivities",
                columns: table => new
                {
                    AccountId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    ActivityId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Group = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    IsCheckpoint = table.Column<bool>(type: "INTEGER", nullable: false),
                    Done = table.Column<bool>(type: "INTEGER", nullable: false),
                    SyncedAtUtc = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountResetActivities", x => new { x.AccountId, x.Kind, x.ActivityId });
                });

            migrationBuilder.CreateTable(
                name: "AccountWizardsVaultMetas",
                columns: table => new
                {
                    AccountId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Period = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    ProgressCurrent = table.Column<int>(type: "INTEGER", nullable: false),
                    ProgressComplete = table.Column<int>(type: "INTEGER", nullable: false),
                    RewardItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    RewardAstral = table.Column<int>(type: "INTEGER", nullable: false),
                    RewardClaimed = table.Column<bool>(type: "INTEGER", nullable: false),
                    SyncedAtUtc = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountWizardsVaultMetas", x => new { x.AccountId, x.Period });
                });

            migrationBuilder.CreateTable(
                name: "AccountWizardsVaultObjectives",
                columns: table => new
                {
                    AccountId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Period = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    ObjectiveId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Track = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    Acclaim = table.Column<int>(type: "INTEGER", nullable: false),
                    ProgressCurrent = table.Column<int>(type: "INTEGER", nullable: false),
                    ProgressComplete = table.Column<int>(type: "INTEGER", nullable: false),
                    Claimed = table.Column<bool>(type: "INTEGER", nullable: false),
                    SyncedAtUtc = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountWizardsVaultObjectives", x => new { x.AccountId, x.Period, x.ObjectiveId });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountResetActivities");

            migrationBuilder.DropTable(
                name: "AccountWizardsVaultMetas");

            migrationBuilder.DropTable(
                name: "AccountWizardsVaultObjectives");
        }
    }
}
