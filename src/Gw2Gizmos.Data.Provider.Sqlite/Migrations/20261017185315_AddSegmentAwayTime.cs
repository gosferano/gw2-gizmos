using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gw2Gizmos.Data.Provider.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddSegmentAwayTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "AwaySinceUtc",
                table: "CharacterSegments",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "AwayTime",
                table: "CharacterSegments",
                type: "TEXT",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AwaySinceUtc",
                table: "CharacterSegments");

            migrationBuilder.DropColumn(
                name: "AwayTime",
                table: "CharacterSegments");
        }
    }
}
