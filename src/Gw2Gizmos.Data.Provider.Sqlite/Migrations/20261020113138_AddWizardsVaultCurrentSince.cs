using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gw2Gizmos.Data.Provider.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddWizardsVaultCurrentSince : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CurrentSinceUtc",
                table: "AccountWizardsVaultMetas",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            // A set already stored is taken as current since it was last synced: right for a set the account got
            // after the reset, and a set from before it is replaced (and corrected) at the next login anyway.
            migrationBuilder.Sql("UPDATE AccountWizardsVaultMetas SET CurrentSinceUtc = SyncedAtUtc;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentSinceUtc",
                table: "AccountWizardsVaultMetas");
        }
    }
}
