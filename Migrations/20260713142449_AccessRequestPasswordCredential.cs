using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YourRhythmStudio.Migrations
{
    /// <inheritdoc />
    public partial class AccessRequestPasswordCredential : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PasswordAlgorithm",
                table: "access_requests",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PasswordHashBase64",
                table: "access_requests",
                type: "varchar(512)",
                maxLength: 512,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "PasswordIterations",
                table: "access_requests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PasswordSaltBase64",
                table: "access_requests",
                type: "varchar(256)",
                maxLength: 256,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordUpdatedAtUtc",
                table: "access_requests",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordAlgorithm",
                table: "access_requests");

            migrationBuilder.DropColumn(
                name: "PasswordHashBase64",
                table: "access_requests");

            migrationBuilder.DropColumn(
                name: "PasswordIterations",
                table: "access_requests");

            migrationBuilder.DropColumn(
                name: "PasswordSaltBase64",
                table: "access_requests");

            migrationBuilder.DropColumn(
                name: "PasswordUpdatedAtUtc",
                table: "access_requests");
        }
    }
}
