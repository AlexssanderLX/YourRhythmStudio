using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YourRhythmStudio.Migrations
{
    /// <inheritdoc />
    public partial class MissionCentralQuestionOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OptionsJson",
                table: "mission_questions",
                type: "varchar(1600)",
                maxLength: 1600,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OptionsJson",
                table: "mission_questions");
        }
    }
}
