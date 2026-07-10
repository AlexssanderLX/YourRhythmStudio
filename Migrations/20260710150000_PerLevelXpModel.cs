using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YourRhythmStudio.Migrations
{
    /// <inheritdoc />
    public partial class PerLevelXpModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add the new per-level XP column, defaulting to 0.
            migrationBuilder.AddColumn<int>(
                name: "CurrentLevelXp",
                table: "student_profiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Migrate existing students: derive CurrentLevelXp from their cumulative CurrentXp
            // using the OLD level boundaries (level 1 ended at 500, level 2 at 2500, etc.).
            migrationBuilder.Sql(@"
UPDATE student_profiles SET CurrentLevelXp =
  GREATEST(0,
    CASE CurrentLevel
      WHEN 1 THEN LEAST(CurrentXp, 500)
      WHEN 2 THEN LEAST(CurrentXp - 500,   2500)
      WHEN 3 THEN LEAST(CurrentXp - 2500,  7500)
      WHEN 4 THEN LEAST(CurrentXp - 7500,  15000)
      WHEN 5 THEN LEAST(CurrentXp - 15000, 25000)
      ELSE 0
    END
  );
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentLevelXp",
                table: "student_profiles");
        }
    }
}
