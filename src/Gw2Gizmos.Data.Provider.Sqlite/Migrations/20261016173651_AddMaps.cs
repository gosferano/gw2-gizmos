using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gw2Gizmos.Data.Provider.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddMaps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Maps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    MinLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    DefaultFloor = table.Column<int>(type: "INTEGER", nullable: false),
                    Floors = table.Column<string>(type: "TEXT", nullable: false),
                    RegionId = table.Column<int>(type: "INTEGER", nullable: true),
                    RegionName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    ContinentId = table.Column<int>(type: "INTEGER", nullable: true),
                    ContinentName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    ContinentRect = table.Column<string>(type: "TEXT", nullable: false),
                    MapRect = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Maps", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Maps");
        }
    }
}
