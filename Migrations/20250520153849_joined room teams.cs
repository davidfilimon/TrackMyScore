using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrackMyScore.Migrations
{
    /// <inheritdoc />
    public partial class joinedroomteams : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TeamId",
                table: "JoinRooms",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_JoinRooms_TeamId",
                table: "JoinRooms",
                column: "TeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_JoinRooms_Teams_TeamId",
                table: "JoinRooms",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JoinRooms_Teams_TeamId",
                table: "JoinRooms");

            migrationBuilder.DropIndex(
                name: "IX_JoinRooms_TeamId",
                table: "JoinRooms");

            migrationBuilder.DropColumn(
                name: "TeamId",
                table: "JoinRooms");
        }
    }
}
