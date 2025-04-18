using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrackMyScore.Migrations
{
    /// <inheritdoc />
    public partial class dbChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Participants_ParticipantId",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Participants_Rooms_RoomId",
                table: "Participants");

            migrationBuilder.DropForeignKey(
                name: "FK_Rooms_Users_PlayerId",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "isWinner",
                table: "Matches");

            migrationBuilder.RenameColumn(
                name: "PlayerId",
                table: "Rooms",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Rooms_PlayerId",
                table: "Rooms",
                newName: "IX_Rooms_UserId");

            migrationBuilder.RenameColumn(
                name: "RoomId",
                table: "Participants",
                newName: "MatchId");

            migrationBuilder.RenameIndex(
                name: "IX_Participants_RoomId",
                table: "Participants",
                newName: "IX_Participants_MatchId");

            migrationBuilder.RenameColumn(
                name: "ParticipantId",
                table: "Matches",
                newName: "RoomId");

            migrationBuilder.RenameIndex(
                name: "IX_Matches_ParticipantId",
                table: "Matches",
                newName: "IX_Matches_RoomId");

            migrationBuilder.AddColumn<bool>(
                name: "IsWinner",
                table: "Participants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Rooms_RoomId",
                table: "Matches",
                column: "RoomId",
                principalTable: "Rooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Participants_Matches_MatchId",
                table: "Participants",
                column: "MatchId",
                principalTable: "Matches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Rooms_Users_UserId",
                table: "Rooms",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Rooms_RoomId",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Participants_Matches_MatchId",
                table: "Participants");

            migrationBuilder.DropForeignKey(
                name: "FK_Rooms_Users_UserId",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "IsWinner",
                table: "Participants");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Rooms",
                newName: "PlayerId");

            migrationBuilder.RenameIndex(
                name: "IX_Rooms_UserId",
                table: "Rooms",
                newName: "IX_Rooms_PlayerId");

            migrationBuilder.RenameColumn(
                name: "MatchId",
                table: "Participants",
                newName: "RoomId");

            migrationBuilder.RenameIndex(
                name: "IX_Participants_MatchId",
                table: "Participants",
                newName: "IX_Participants_RoomId");

            migrationBuilder.RenameColumn(
                name: "RoomId",
                table: "Matches",
                newName: "ParticipantId");

            migrationBuilder.RenameIndex(
                name: "IX_Matches_RoomId",
                table: "Matches",
                newName: "IX_Matches_ParticipantId");

            migrationBuilder.AddColumn<bool>(
                name: "isWinner",
                table: "Matches",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Participants_ParticipantId",
                table: "Matches",
                column: "ParticipantId",
                principalTable: "Participants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Participants_Rooms_RoomId",
                table: "Participants",
                column: "RoomId",
                principalTable: "Rooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Rooms_Users_PlayerId",
                table: "Rooms",
                column: "PlayerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
