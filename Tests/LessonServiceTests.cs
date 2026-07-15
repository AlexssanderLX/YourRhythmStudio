using Microsoft.EntityFrameworkCore;
using YourRhythmStudio.Application.Learning;
using YourRhythmStudio.Application.Users;
using YourRhythmStudio.Domain;
using YourRhythmStudio.Domain.Learning;
using YourRhythmStudio.Domain.Users;
using YourRhythmStudio.Infrastructure.Data;

namespace YourRhythmStudio.Tests;

public sealed class LessonServiceTests
{
    [Fact]
    public async Task ListLessonsForTeacher_ReturnsOnlyAuthorizedActiveStudentLessons()
    {
        await using var db = CreateDb();
        var ctx = await SeedAsync(db);
        var service = new LessonService(db);

        await service.CreateLessonAsync(ctx.TeacherProfile, new CreateLessonRequest(
            ctx.StudentProfile.Id, "Aula autorizada", DateTime.UtcNow, "Escalas"));

        var otherLesson = new Lesson(
            ctx.OtherSchool.Id,
            ctx.OtherTeacherProfile.Id,
            ctx.OtherStudentProfile.Id,
            "Aula de outra escola",
            DateTime.UtcNow,
            DateTime.UtcNow);
        db.Lessons.Add(otherLesson);
        await db.SaveChangesAsync();

        var lessons = await service.ListLessonsForTeacherAsync(ctx.TeacherProfile);

        var lesson = Assert.Single(lessons);
        Assert.Equal("Aula autorizada", lesson.Title);
        Assert.Equal(ctx.StudentProfile.Id, lesson.StudentProfileId);
    }

    [Fact]
    public async Task CreateLessonAsync_WithAuthorizedStudent_CreatesLesson()
    {
        await using var db = CreateDb();
        var ctx = await SeedAsync(db);
        var service = new LessonService(db);

        var result = await service.CreateLessonAsync(ctx.TeacherProfile, new CreateLessonRequest(
            ctx.StudentProfile.Id, "Ritmo", DateTime.UtcNow, "Semicolcheias"));

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Ritmo", result.Title);
        Assert.Single(db.Lessons);
    }

    [Fact]
    public async Task CreateLessonAsync_WithUnauthorizedStudent_BlocksAccess()
    {
        await using var db = CreateDb();
        var ctx = await SeedAsync(db);
        var service = new LessonService(db);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.CreateLessonAsync(ctx.TeacherProfile, new CreateLessonRequest(
                ctx.OtherStudentProfile.Id, "Invalida", DateTime.UtcNow, null)));
    }

    [Fact]
    public async Task UpdateLessonAsync_WithAuthorizedLesson_UpdatesLesson()
    {
        await using var db = CreateDb();
        var ctx = await SeedAsync(db);
        var service = new LessonService(db);
        var created = await service.CreateLessonAsync(ctx.TeacherProfile, new CreateLessonRequest(
            ctx.StudentProfile.Id, "Original", DateTime.UtcNow, null));

        var updated = await service.UpdateLessonAsync(ctx.TeacherProfile, new UpdateLessonRequest(
            ctx.StudentProfile.Id, created.Id, "Atualizada", DateTime.UtcNow.AddHours(2), "Novo resumo"));

        Assert.Equal("Atualizada", updated.Title);
        Assert.Equal("Novo resumo", updated.Notes);
    }

    [Fact]
    public async Task UpdateLessonAsync_WithOtherStudent_BlocksAccess()
    {
        await using var db = CreateDb();
        var ctx = await SeedAsync(db);
        var service = new LessonService(db);
        var created = await service.CreateLessonAsync(ctx.TeacherProfile, new CreateLessonRequest(
            ctx.StudentProfile.Id, "Original", DateTime.UtcNow, null));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.UpdateLessonAsync(ctx.TeacherProfile, new UpdateLessonRequest(
                ctx.OtherStudentProfile.Id, created.Id, "Ataque", DateTime.UtcNow, null)));
    }

    [Fact]
    public async Task DeleteLessonAsync_WithAuthorizedLesson_DeletesLesson()
    {
        await using var db = CreateDb();
        var ctx = await SeedAsync(db);
        var service = new LessonService(db);
        var created = await service.CreateLessonAsync(ctx.TeacherProfile, new CreateLessonRequest(
            ctx.StudentProfile.Id, "Para excluir", DateTime.UtcNow, null));

        await service.DeleteLessonAsync(ctx.TeacherProfile, ctx.StudentProfile.Id, created.Id);

        Assert.Empty(db.Lessons);
    }

    [Fact]
    public async Task DeleteLessonAsync_WhenRepeated_ReturnsNotFound()
    {
        await using var db = CreateDb();
        var ctx = await SeedAsync(db);
        var service = new LessonService(db);
        var created = await service.CreateLessonAsync(ctx.TeacherProfile, new CreateLessonRequest(
            ctx.StudentProfile.Id, "Para excluir", DateTime.UtcNow, null));

        await service.DeleteLessonAsync(ctx.TeacherProfile, ctx.StudentProfile.Id, created.Id);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.DeleteLessonAsync(ctx.TeacherProfile, ctx.StudentProfile.Id, created.Id));
    }

    [Fact]
    public async Task GetLessonDetailAsync_WithMissingId_ReturnsNotFound()
    {
        await using var db = CreateDb();
        var ctx = await SeedAsync(db);
        var service = new LessonService(db);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.GetLessonDetailAsync(ctx.TeacherProfile, ctx.StudentProfile.Id, Guid.NewGuid()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateLessonAsync_WithInvalidTitle_ValidatesRequiredTitle(string title)
    {
        await using var db = CreateDb();
        var ctx = await SeedAsync(db);
        var service = new LessonService(db);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateLessonAsync(ctx.TeacherProfile, new CreateLessonRequest(
                ctx.StudentProfile.Id, title, DateTime.UtcNow, null)));
    }

    private static YourRhythmDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YourRhythmDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new YourRhythmDbContext(options);
    }

    private static async Task<TestContext> SeedAsync(YourRhythmDbContext db)
    {
        var school = new School { Name = "YourRhythm", Slug = Guid.NewGuid().ToString("N"), PrimaryEmail = "school@example.test" };
        var otherSchool = new School { Name = "Outra", Slug = Guid.NewGuid().ToString("N"), PrimaryEmail = "other@example.test" };

        var teacherUser = new SchoolUser
        {
            SchoolId = school.Id,
            DisplayName = "Professor",
            Email = "prof@example.test",
            Role = YourRhythmRoles.Teacher
        };
        var teacher = new TeacherProfile
        {
            SchoolId = school.Id,
            SchoolUserId = teacherUser.Id,
            InstrumentFocus = "Piano"
        };
        var studentUser = new SchoolUser
        {
            SchoolId = school.Id,
            DisplayName = "Aluno",
            Email = "student@example.test",
            Role = YourRhythmRoles.Student
        };
        var student = new StudentProfile
        {
            SchoolId = school.Id,
            SchoolUserId = studentUser.Id,
            Instrument = "Piano",
            Level = "1"
        };
        var link = new TeacherStudent(school.Id, teacher.Id, student.Id, DateTime.UtcNow);

        var otherTeacherUser = new SchoolUser
        {
            SchoolId = otherSchool.Id,
            DisplayName = "Outro Professor",
            Email = "other-prof@example.test",
            Role = YourRhythmRoles.Teacher
        };
        var otherTeacher = new TeacherProfile
        {
            SchoolId = otherSchool.Id,
            SchoolUserId = otherTeacherUser.Id,
            InstrumentFocus = "Violao"
        };
        var otherStudentUser = new SchoolUser
        {
            SchoolId = otherSchool.Id,
            DisplayName = "Outro Aluno",
            Email = "other-student@example.test",
            Role = YourRhythmRoles.Student
        };
        var otherStudent = new StudentProfile
        {
            SchoolId = otherSchool.Id,
            SchoolUserId = otherStudentUser.Id,
            Instrument = "Violao",
            Level = "1"
        };
        var otherLink = new TeacherStudent(otherSchool.Id, otherTeacher.Id, otherStudent.Id, DateTime.UtcNow);

        db.AddRange(school, otherSchool, teacherUser, teacher, studentUser, student, link,
            otherTeacherUser, otherTeacher, otherStudentUser, otherStudent, otherLink);
        await db.SaveChangesAsync();

        return new TestContext(
            new AuthenticatedUserProfile(
                Guid.NewGuid(),
                teacherUser.Email,
                teacherUser.DisplayName,
                YourRhythmRoles.Teacher,
                school.Id,
                teacherUser.Id,
                teacher.Id,
                null),
            school,
            teacher,
            student,
            otherSchool,
            otherTeacher,
            otherStudent);
    }

    private sealed record TestContext(
        AuthenticatedUserProfile TeacherProfile,
        School School,
        TeacherProfile Teacher,
        StudentProfile StudentProfile,
        School OtherSchool,
        TeacherProfile OtherTeacherProfile,
        StudentProfile OtherStudentProfile);
}
