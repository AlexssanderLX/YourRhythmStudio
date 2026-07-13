using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YourRhythmStudio.Migrations
{
    /// <inheritdoc />
    public partial class AddSchoolUserProfilePhotoPath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfilePhotoPath",
                table: "school_users",
                type: "varchar(260)",
                maxLength: 260,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfilePhotoPath",
                table: "school_users");
        }
    }
}
