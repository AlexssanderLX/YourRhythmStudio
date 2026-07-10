using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YourRhythmStudio.Migrations
{
    /// <inheritdoc />
    public partial class EvolutionSkillsLevelsProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AchievementText",
                table: "skills",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ConquestCriteria",
                table: "skills",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "skills",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "skills",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "skills",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "SkillRewardId",
                table: "assignments",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateTable(
                name: "level_configs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    SchoolId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    TeacherProfileId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Level = table.Column<int>(type: "int", nullable: false),
                    Subtitle = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TeacherExpectations = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Objectives = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConquestMessage = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OrientationMessage = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_level_configs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_level_configs_schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_level_configs_teacher_profiles_TeacherProfileId",
                        column: x => x.TeacherProfileId,
                        principalTable: "teacher_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "level_up_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    SchoolId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    StudentProfileId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    FromLevel = table.Column<int>(type: "int", nullable: false),
                    ToLevel = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    SeenAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_level_up_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_level_up_events_schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_level_up_events_student_profiles_StudentProfileId",
                        column: x => x.StudentProfileId,
                        principalTable: "student_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_assignments_SkillRewardId",
                table: "assignments",
                column: "SkillRewardId");

            migrationBuilder.CreateIndex(
                name: "IX_level_configs_SchoolId_TeacherProfileId_Level",
                table: "level_configs",
                columns: new[] { "SchoolId", "TeacherProfileId", "Level" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_level_configs_TeacherProfileId",
                table: "level_configs",
                column: "TeacherProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_level_up_events_SchoolId_StudentProfileId_SeenAtUtc",
                table: "level_up_events",
                columns: new[] { "SchoolId", "StudentProfileId", "SeenAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_level_up_events_StudentProfileId",
                table: "level_up_events",
                column: "StudentProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_assignments_skills_SkillRewardId",
                table: "assignments",
                column: "SkillRewardId",
                principalTable: "skills",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_assignments_skills_SkillRewardId",
                table: "assignments");

            migrationBuilder.DropTable(
                name: "level_configs");

            migrationBuilder.DropTable(
                name: "level_up_events");

            migrationBuilder.DropIndex(
                name: "IX_assignments_SkillRewardId",
                table: "assignments");

            migrationBuilder.DropColumn(
                name: "AchievementText",
                table: "skills");

            migrationBuilder.DropColumn(
                name: "ConquestCriteria",
                table: "skills");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "skills");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "skills");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "skills");

            migrationBuilder.DropColumn(
                name: "SkillRewardId",
                table: "assignments");
        }
    }
}
