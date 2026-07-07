using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YourRhythmStudio.Migrations
{
    /// <inheritdoc />
    public partial class CompleteProfessorStudentBackendMvp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "schools",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Name = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Slug = table.Column<string>(type: "varchar(180)", maxLength: 180, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PrimaryEmail = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OwnerAccountId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_schools", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "school_users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    SchoolId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    AccountId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    DisplayName = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Role = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_school_users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_school_users_schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "student_profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    SchoolId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    SchoolUserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Instrument = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Level = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Notes = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CurrentXp = table.Column<int>(type: "int", nullable: false),
                    CurrentLevel = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_profiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_student_profiles_school_users_SchoolUserId",
                        column: x => x.SchoolUserId,
                        principalTable: "school_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_student_profiles_schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "teacher_profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    SchoolId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    SchoolUserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    InstrumentFocus = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Bio = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CanManageStudents = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teacher_profiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_teacher_profiles_school_users_SchoolUserId",
                        column: x => x.SchoolUserId,
                        principalTable: "school_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_teacher_profiles_schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "lessons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    SchoolId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    TeacherProfileId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    StudentProfileId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Title = table.Column<string>(type: "varchar(180)", maxLength: 180, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LessonDateUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lessons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_lessons_schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_lessons_student_profiles_StudentProfileId",
                        column: x => x.StudentProfileId,
                        principalTable: "student_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_lessons_teacher_profiles_TeacherProfileId",
                        column: x => x.TeacherProfileId,
                        principalTable: "teacher_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "repertoire_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    SchoolId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    TeacherProfileId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    StudentProfileId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Title = table.Column<string>(type: "varchar(180)", maxLength: 180, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ComposerOrArtist = table.Column<string>(type: "varchar(180)", maxLength: 180, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Instrument = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Level = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ProgressPercent = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReferenceUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_repertoire_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_repertoire_items_schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_repertoire_items_student_profiles_StudentProfileId",
                        column: x => x.StudentProfileId,
                        principalTable: "student_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_repertoire_items_teacher_profiles_TeacherProfileId",
                        column: x => x.TeacherProfileId,
                        principalTable: "teacher_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "teacher_students",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    SchoolId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    TeacherProfileId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    StudentProfileId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DeactivatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teacher_students", x => x.Id);
                    table.ForeignKey(
                        name: "FK_teacher_students_schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_teacher_students_student_profiles_StudentProfileId",
                        column: x => x.StudentProfileId,
                        principalTable: "student_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_teacher_students_teacher_profiles_TeacherProfileId",
                        column: x => x.TeacherProfileId,
                        principalTable: "teacher_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "xp_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    SchoolId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    StudentProfileId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    TeacherProfileId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    SourceId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_xp_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_xp_events_schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_xp_events_student_profiles_StudentProfileId",
                        column: x => x.StudentProfileId,
                        principalTable: "student_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_xp_events_teacher_profiles_TeacherProfileId",
                        column: x => x.TeacherProfileId,
                        principalTable: "teacher_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "assignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    SchoolId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    TeacherProfileId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    StudentProfileId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    LessonId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    RepertoireItemId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    Title = table.Column<string>(type: "varchar(180)", maxLength: 180, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DueAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TargetMinutes = table.Column<int>(type: "int", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    XpReward = table.Column<int>(type: "int", nullable: false),
                    XpGranted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_assignments_lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_assignments_repertoire_items_RepertoireItemId",
                        column: x => x.RepertoireItemId,
                        principalTable: "repertoire_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_assignments_schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_assignments_student_profiles_StudentProfileId",
                        column: x => x.StudentProfileId,
                        principalTable: "student_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_assignments_teacher_profiles_TeacherProfileId",
                        column: x => x.TeacherProfileId,
                        principalTable: "teacher_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "feedback_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    SchoolId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    TeacherProfileId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    StudentProfileId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    LessonId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    AssignmentId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    RepertoireItemId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    Message = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VisibleToStudent = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_feedback_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_feedback_entries_assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_feedback_entries_lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_feedback_entries_repertoire_items_RepertoireItemId",
                        column: x => x.RepertoireItemId,
                        principalTable: "repertoire_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_feedback_entries_schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_feedback_entries_student_profiles_StudentProfileId",
                        column: x => x.StudentProfileId,
                        principalTable: "student_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_feedback_entries_teacher_profiles_TeacherProfileId",
                        column: x => x.TeacherProfileId,
                        principalTable: "teacher_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_assignments_LessonId",
                table: "assignments",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "IX_assignments_RepertoireItemId",
                table: "assignments",
                column: "RepertoireItemId");

            migrationBuilder.CreateIndex(
                name: "IX_assignments_SchoolId",
                table: "assignments",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_assignments_SchoolId_StudentProfileId_Status",
                table: "assignments",
                columns: new[] { "SchoolId", "StudentProfileId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_assignments_StudentProfileId",
                table: "assignments",
                column: "StudentProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_assignments_TeacherProfileId",
                table: "assignments",
                column: "TeacherProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_feedback_entries_AssignmentId",
                table: "feedback_entries",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_feedback_entries_LessonId",
                table: "feedback_entries",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "IX_feedback_entries_RepertoireItemId",
                table: "feedback_entries",
                column: "RepertoireItemId");

            migrationBuilder.CreateIndex(
                name: "IX_feedback_entries_SchoolId",
                table: "feedback_entries",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_feedback_entries_SchoolId_StudentProfileId_VisibleToStudent",
                table: "feedback_entries",
                columns: new[] { "SchoolId", "StudentProfileId", "VisibleToStudent" });

            migrationBuilder.CreateIndex(
                name: "IX_feedback_entries_StudentProfileId",
                table: "feedback_entries",
                column: "StudentProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_feedback_entries_TeacherProfileId",
                table: "feedback_entries",
                column: "TeacherProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_lessons_SchoolId",
                table: "lessons",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_lessons_SchoolId_StudentProfileId_LessonDateUtc",
                table: "lessons",
                columns: new[] { "SchoolId", "StudentProfileId", "LessonDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_lessons_StudentProfileId",
                table: "lessons",
                column: "StudentProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_lessons_TeacherProfileId",
                table: "lessons",
                column: "TeacherProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_repertoire_items_SchoolId",
                table: "repertoire_items",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_repertoire_items_SchoolId_StudentProfileId_Status",
                table: "repertoire_items",
                columns: new[] { "SchoolId", "StudentProfileId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_repertoire_items_StudentProfileId",
                table: "repertoire_items",
                column: "StudentProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_repertoire_items_TeacherProfileId",
                table: "repertoire_items",
                column: "TeacherProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_school_users_SchoolId_Email",
                table: "school_users",
                columns: new[] { "SchoolId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_schools_Slug",
                table: "schools",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_student_profiles_SchoolId",
                table: "student_profiles",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_student_profiles_SchoolUserId",
                table: "student_profiles",
                column: "SchoolUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_teacher_profiles_SchoolId",
                table: "teacher_profiles",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_teacher_profiles_SchoolUserId",
                table: "teacher_profiles",
                column: "SchoolUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_teacher_students_SchoolId",
                table: "teacher_students",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_teacher_students_SchoolId_TeacherProfileId_StudentProfileId",
                table: "teacher_students",
                columns: new[] { "SchoolId", "TeacherProfileId", "StudentProfileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_teacher_students_StudentProfileId",
                table: "teacher_students",
                column: "StudentProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_teacher_students_TeacherProfileId",
                table: "teacher_students",
                column: "TeacherProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_xp_events_SchoolId",
                table: "xp_events",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_xp_events_SchoolId_StudentProfileId_CreatedAtUtc",
                table: "xp_events",
                columns: new[] { "SchoolId", "StudentProfileId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_xp_events_SourceId",
                table: "xp_events",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_xp_events_StudentProfileId",
                table: "xp_events",
                column: "StudentProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_xp_events_TeacherProfileId",
                table: "xp_events",
                column: "TeacherProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "feedback_entries");

            migrationBuilder.DropTable(
                name: "teacher_students");

            migrationBuilder.DropTable(
                name: "xp_events");

            migrationBuilder.DropTable(
                name: "assignments");

            migrationBuilder.DropTable(
                name: "lessons");

            migrationBuilder.DropTable(
                name: "repertoire_items");

            migrationBuilder.DropTable(
                name: "student_profiles");

            migrationBuilder.DropTable(
                name: "teacher_profiles");

            migrationBuilder.DropTable(
                name: "school_users");

            migrationBuilder.DropTable(
                name: "schools");
        }
    }
}
