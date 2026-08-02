using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PongChampions.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMaxScoreToRoom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxScore",
                table: "Rooms",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxScore",
                table: "Rooms");
        }
    }
}
