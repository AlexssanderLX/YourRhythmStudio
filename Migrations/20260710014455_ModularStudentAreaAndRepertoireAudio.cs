using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YourRhythmStudio.Migrations
{
    /// <inheritdoc />
    public partial class ModularStudentAreaAndRepertoireAudio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Level",
                table: "repertoire_items");

            migrationBuilder.DropColumn(
                name: "Instrument",
                table: "repertoire_items");

            migrationBuilder.DropColumn(
                name: "ComposerOrArtist",
                table: "repertoire_items");

            migrationBuilder.DropColumn(
                name: "DurationMinutes",
                table: "lessons");

            migrationBuilder.DropColumn(
                name: "TargetMinutes",
                table: "assignments");

            migrationBuilder.AlterColumn<int>(
                name: "SkillType",
                table: "skills",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 3);

            migrationBuilder.AddColumn<string>(
                name: "AudioOriginalFileName",
                table: "repertoire_items",
                type: "varchar(260)",
                maxLength: 260,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "AudioStoredFileName",
                table: "repertoire_items",
                type: "varchar(180)",
                maxLength: 180,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "AudioContentType",
                table: "repertoire_items",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<long>(
                name: "AudioSizeBytes",
                table: "repertoire_items",
                type: "bigint",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Rarity",
                table: "assignments",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AudioOriginalFileName",
                table: "repertoire_items");

            migrationBuilder.DropColumn(
                name: "AudioSizeBytes",
                table: "repertoire_items");

            migrationBuilder.DropColumn(
                name: "AudioStoredFileName",
                table: "repertoire_items");

            migrationBuilder.DropColumn(
                name: "AudioContentType",
                table: "repertoire_items");

            migrationBuilder.AlterColumn<int>(
                name: "SkillType",
                table: "skills",
                type: "int",
                nullable: false,
                defaultValue: 3,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "Level",
                table: "repertoire_items",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Instrument",
                table: "repertoire_items",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ComposerOrArtist",
                table: "repertoire_items",
                type: "varchar(180)",
                maxLength: 180,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "DurationMinutes",
                table: "lessons",
                type: "int",
                nullable: false,
                defaultValue: 60);

            migrationBuilder.AlterColumn<int>(
                name: "Rarity",
                table: "assignments",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "TargetMinutes",
                table: "assignments",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
