using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PongChampions.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEntityConfigurations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Players_Users_UserId",
                table: "Players");

            migrationBuilder.DropForeignKey(
                name: "FK_Rooms_Players_GuestPlayerId",
                table: "Rooms");

            migrationBuilder.DropForeignKey(
                name: "FK_Rooms_Players_HostPlayerId",
                table: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_Players_UserId",
                table: "Players");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "Users",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "Users",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "Users",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Country",
                table: "Users",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Rooms",
                type: "character varying(6)",
                maxLength: 6,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "Players",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_Code",
                table: "Rooms",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_UserId",
                table: "Players",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Matches_Player1Id",
                table: "Matches",
                column: "Player1Id");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_Player2Id",
                table: "Matches",
                column: "Player2Id");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_RoomId",
                table: "Matches",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_WinnerPlayerId",
                table: "Matches",
                column: "WinnerPlayerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Players_Player1Id",
                table: "Matches",
                column: "Player1Id",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Players_Player2Id",
                table: "Matches",
                column: "Player2Id",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Players_WinnerPlayerId",
                table: "Matches",
                column: "WinnerPlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Rooms_RoomId",
                table: "Matches",
                column: "RoomId",
                principalTable: "Rooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Players_Users_UserId",
                table: "Players",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Rooms_Players_GuestPlayerId",
                table: "Rooms",
                column: "GuestPlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Rooms_Players_HostPlayerId",
                table: "Rooms",
                column: "HostPlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Players_Player1Id",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Players_Player2Id",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Players_WinnerPlayerId",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Rooms_RoomId",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Players_Users_UserId",
                table: "Players");

            migrationBuilder.DropForeignKey(
                name: "FK_Rooms_Players_GuestPlayerId",
                table: "Rooms");

            migrationBuilder.DropForeignKey(
                name: "FK_Rooms_Players_HostPlayerId",
                table: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_Users_Username",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Rooms_Code",
                table: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_Players_UserId",
                table: "Players");

            migrationBuilder.DropIndex(
                name: "IX_Matches_Player1Id",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_Matches_Player2Id",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_Matches_RoomId",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_Matches_WinnerPlayerId",
                table: "Matches");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "Users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<int>(
                name: "Role",
                table: "Users",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "Users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<string>(
                name: "Country",
                table: "Users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Rooms",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(6)",
                oldMaxLength: 6);

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "Players",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.CreateIndex(
                name: "IX_Players_UserId",
                table: "Players",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Players_Users_UserId",
                table: "Players",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");

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
    }
}
