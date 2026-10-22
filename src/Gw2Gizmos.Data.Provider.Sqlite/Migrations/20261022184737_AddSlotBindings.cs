using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gw2Gizmos.Data.Provider.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddSlotBindings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Binding",
                table: "CharacterItemSlots",
                type: "TEXT",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Binding",
                table: "AccountContainerSlots",
                type: "TEXT",
                maxLength: 16,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Binding",
                table: "CharacterItemSlots");

            migrationBuilder.DropColumn(
                name: "Binding",
                table: "AccountContainerSlots");
        }
    }
}
