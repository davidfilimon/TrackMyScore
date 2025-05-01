using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrackMyScore.Migrations
{
    /// <inheritdoc />
    public partial class databaseChange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Rooms_RoomId",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Teams_TeamId",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Participants_Matches_MatchId",
                table: "Participants");

            migrationBuilder.DropIndex(
                name: "IX_Participants_MatchId",
                table: "Participants");

            migrationBuilder.DropIndex(
                name: "IX_Matches_RoomId",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_Matches_TeamId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "RoomId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "TeamId",
                table: "Matches");

            migrationBuilder.RenameColumn(
                name: "MatchId",
                table: "Participants",
                newName: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Participants_RoomId",
                table: "Participants",
                column: "RoomId");

            migrationBuilder.AddForeignKey(
                name: "FK_Participants_Rooms_RoomId",
                table: "Participants",
                column: "RoomId",
                principalTable: "Rooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Participants_Rooms_RoomId",
                table: "Participants");

            migrationBuilder.DropIndex(
                name: "IX_Participants_RoomId",
                table: "Participants");

            migrationBuilder.RenameColumn(
                name: "RoomId",
                table: "Participants",
                newName: "MatchId");

            migrationBuilder.AddColumn<int>(
                name: "RoomId",
                table: "Matches",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TeamId",
                table: "Matches",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Participants_MatchId",
                table: "Participants",
                column: "MatchId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Matches_RoomId",
                table: "Matches",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_TeamId",
                table: "Matches",
                column: "TeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Rooms_RoomId",
                table: "Matches",
                column: "RoomId",
                principalTable: "Rooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Teams_TeamId",
                table: "Matches",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Participants_Matches_MatchId",
                table: "Participants",
                column: "MatchId",
                principalTable: "Matches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
