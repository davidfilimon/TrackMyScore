using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrackMyScore.Migrations
{
    /// <inheritdoc />
    public partial class dbFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "Tournaments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Tournaments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
