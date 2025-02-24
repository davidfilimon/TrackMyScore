using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrackMyScore.Migrations
{
    /// <inheritdoc />
    public partial class addedOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Followers_Users_FollowerId",
                table: "Followers");

            migrationBuilder.DropForeignKey(
                name: "FK_Followers_Users_FollowingId",
                table: "Followers");

            migrationBuilder.DropIndex(
                name: "IX_Followers_FollowerId",
                table: "Followers");

            migrationBuilder.DropIndex(
                name: "IX_Followers_FollowingId",
                table: "Followers");

            migrationBuilder.AddColumn<string>(
                name: "Author",
                table: "Games",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsOfficial",
                table: "Games",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Author",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "IsOfficial",
                table: "Games");

            migrationBuilder.CreateIndex(
                name: "IX_Followers_FollowerId",
                table: "Followers",
                column: "FollowerId");

            migrationBuilder.CreateIndex(
                name: "IX_Followers_FollowingId",
                table: "Followers",
                column: "FollowingId");

            migrationBuilder.AddForeignKey(
                name: "FK_Followers_Users_FollowerId",
                table: "Followers",
                column: "FollowerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Followers_Users_FollowingId",
                table: "Followers",
                column: "FollowingId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
