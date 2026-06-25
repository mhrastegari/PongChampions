using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PongChampions.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingPropertiesToMatchAndRoom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "Matches");

            migrationBuilder.RenameColumn(
                name: "MaxScore",
                table: "Matches",
                newName: "Player2Score");

            migrationBuilder.AddColumn<Guid>(
                name: "GuestPlayerId",
                table: "Rooms",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "WinnerPlayerId",
                table: "Matches",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Player1Score",
                table: "Matches",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_GuestPlayerId",
                table: "Rooms",
                column: "GuestPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_HostPlayerId",
                table: "Rooms",
                column: "HostPlayerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Rooms_Players_GuestPlayerId",
                table: "Rooms",
                column: "GuestPlayerId",
                principalTable: "Players",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Rooms_Players_HostPlayerId",
                table: "Rooms",
                column: "HostPlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rooms_Players_GuestPlayerId",
                table: "Rooms");

            migrationBuilder.DropForeignKey(
                name: "FK_Rooms_Players_HostPlayerId",
                table: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_Rooms_GuestPlayerId",
                table: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_Rooms_HostPlayerId",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "GuestPlayerId",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "Player1Score",
                table: "Matches");

            migrationBuilder.RenameColumn(
                name: "Player2Score",
                table: "Matches",
                newName: "MaxScore");

            migrationBuilder.AlterColumn<Guid>(
                name: "WinnerPlayerId",
                table: "Matches",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "Matches",
                type: "timestamp with time zone",
                nullable: true);
        }
    }
}
