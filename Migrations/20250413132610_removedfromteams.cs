using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrackMyScore.Migrations
{
    /// <inheritdoc />
    public partial class removedfromteams : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Participants_ParticipantId",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_Teams_ParticipantId",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "ParticipantId",
                table: "Teams");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParticipantId",
                table: "Teams",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Teams_ParticipantId",
                table: "Teams",
                column: "ParticipantId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Participants_ParticipantId",
                table: "Teams",
                column: "ParticipantId",
                principalTable: "Participants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
