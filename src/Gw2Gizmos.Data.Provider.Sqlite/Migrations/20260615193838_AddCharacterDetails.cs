using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gw2Gizmos.Data.Provider.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddCharacterDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Characters",
                columns: table => new
                {
                    AccountId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Race = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    Gender = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    Profession = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    Guild = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Age = table.Column<int>(type: "INTEGER", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LastModified = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Deaths = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<int>(type: "INTEGER", nullable: true),
                    BuildTabsUnlocked = table.Column<int>(type: "INTEGER", nullable: false),
                    ActiveBuildTab = table.Column<int>(type: "INTEGER", nullable: false),
                    EquipmentTabsUnlocked = table.Column<int>(type: "INTEGER", nullable: false),
                    ActiveEquipmentTab = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Characters", x => new { x.AccountId, x.Name });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Characters");
        }
    }
}
