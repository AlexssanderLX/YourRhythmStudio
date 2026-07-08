using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YourRhythmStudio.Migrations
{
    /// <inheritdoc />
    public partial class AddSchoolPlanCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PlanCode",
                table: "schools",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "professor")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlanCode",
                table: "schools");
        }
    }
}
