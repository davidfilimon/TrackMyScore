using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrackMyScore.Migrations
{
    /// <inheritdoc />
    public partial class dbChanges2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tournaments_Rewards_RewardId",
                table: "Tournaments");

            migrationBuilder.DropTable(
                name: "Rewards");

            migrationBuilder.DropIndex(
                name: "IX_Tournaments_RewardId",
                table: "Tournaments");

            migrationBuilder.RenameColumn(
                name: "RewardId",
                table: "Tournaments",
                newName: "RoomCount");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Tournaments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "HostId",
                table: "Tournaments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Tournaments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Reward",
                table: "Tournaments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "Tournaments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Tournaments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Team",
                table: "Players",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TeamLeader",
                table: "Players",
                type: "bit",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tournaments_HostId",
                table: "Tournaments",
                column: "HostId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tournaments_Users_HostId",
                table: "Tournaments",
                column: "HostId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tournaments_Users_HostId",
                table: "Tournaments");

            migrationBuilder.DropIndex(
                name: "IX_Tournaments_HostId",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "HostId",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "Reward",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "Team",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "TeamLeader",
                table: "Players");

            migrationBuilder.RenameColumn(
                name: "RoomCount",
                table: "Tournaments",
                newName: "RewardId");

            migrationBuilder.CreateTable(
                name: "Rewards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Value = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rewards", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tournaments_RewardId",
                table: "Tournaments",
                column: "RewardId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tournaments_Rewards_RewardId",
                table: "Tournaments",
                column: "RewardId",
                principalTable: "Rewards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
