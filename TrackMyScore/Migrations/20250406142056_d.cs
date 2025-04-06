using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrackMyScore.Migrations
{
    /// <inheritdoc />
    public partial class d : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Teams_TeamsId",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Participants_Teams_TeamId",
                table: "Participants");

            migrationBuilder.DropIndex(
                name: "IX_Matches_TeamsId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "TeamsId",
                table: "Matches");

            migrationBuilder.AlterColumn<int>(
                name: "TeamId",
                table: "Participants",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "TeamId",
                table: "Matches",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Matches_TeamId",
                table: "Matches",
                column: "TeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Teams_TeamId",
                table: "Matches",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Participants_Teams_TeamId",
                table: "Participants",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Teams_TeamId",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Participants_Teams_TeamId",
                table: "Participants");

            migrationBuilder.DropIndex(
                name: "IX_Matches_TeamId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "TeamId",
                table: "Matches");

            migrationBuilder.AlterColumn<int>(
                name: "TeamId",
                table: "Participants",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TeamsId",
                table: "Matches",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Matches_TeamsId",
                table: "Matches",
                column: "TeamsId");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Teams_TeamsId",
                table: "Matches",
                column: "TeamsId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Participants_Teams_TeamId",
                table: "Participants",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
