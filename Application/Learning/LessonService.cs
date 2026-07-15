using Microsoft.EntityFrameworkCore;
using YourRhythmStudio.Application.Users;
using YourRhythmStudio.Domain.Learning;
using YourRhythmStudio.Domain.Learning.Enums;
using YourRhythmStudio.Infrastructure.Data;

namespace YourRhythmStudio.Application.Learning;

public sealed class LessonService
{
    private readonly YourRhythmDbContext _dbContext;

    public LessonService(YourRhythmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<LessonSummary> CreateLessonAsync(
        AuthenticatedUserProfile profile,
        CreateLessonRequest request,
        CancellationToken cancellationToken = default)
    {
        var (schoolId, teacherProfileId) = LearningAuthorization.RequireTeacher(profile);
        await LearningAuthorization.EnsureTeacherCanAccessStudentAsync(
            _dbContext,
            schoolId,
            teacherProfileId,
            request.StudentProfileId,
            cancellationToken);

        var now = DateTime.UtcNow;
        var lesson = new Lesson(
            schoolId,
            teacherProfileId,
            request.StudentProfileId,
            request.Title,
            request.LessonDateUtc,
            now);

        lesson.UpdateDetails(request.Title, request.LessonDateUtc, request.Notes, now);

        _dbContext.Lessons.Add(lesson);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToSummary(lesson);
    }

    public async Task<IReadOnlyCollection<LessonSummary>> ListLessonsForStudentAsync(
        AuthenticatedUserProfile profile,
        Guid studentProfileId,
        CancellationToken cancellationToken = default)
    {
        var (schoolId, teacherProfileId) = LearningAuthorization.RequireTeacher(profile);
        await LearningAuthorization.EnsureTeacherCanAccessStudentAsync(
            _dbContext,
            schoolId,
            teacherProfileId,
            studentProfileId,
            cancellationToken);

        return await _dbContext.Lessons
            .AsNoTracking()
            .Where(lesson => lesson.SchoolId == schoolId
                && lesson.TeacherProfileId == teacherProfileId
                && lesson.StudentProfileId == studentProfileId)
            .OrderByDescending(lesson => lesson.LessonDateUtc)
            .Select(lesson => new LessonSummary(
                lesson.Id,
                lesson.StudentProfileId,
                lesson.Title,
                lesson.LessonDateUtc,
                lesson.CompletedAtUtc,
                lesson.Status,
                lesson.Notes))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<TeacherLessonListItem>> ListLessonsForTeacherAsync(
        AuthenticatedUserProfile profile,
        int skip = 0,
        int take = 25,
        CancellationToken cancellationToken = default)
    {
        var (schoolId, teacherProfileId) = LearningAuthorization.RequireTeacher(profile);
        var safeSkip = Math.Max(0, skip);
        var safeTake = Math.Clamp(take, 1, 50);

        return await (
            from lesson in _dbContext.Lessons.AsNoTracking()
            join link in _dbContext.TeacherStudents.AsNoTracking()
                on new { lesson.SchoolId, lesson.TeacherProfileId, lesson.StudentProfileId }
                equals new { link.SchoolId, link.TeacherProfileId, link.StudentProfileId }
            join student in _dbContext.StudentProfiles.AsNoTracking()
                on lesson.StudentProfileId equals student.Id
            join user in _dbContext.SchoolUsers.AsNoTracking()
                on student.SchoolUserId equals user.Id
            where lesson.SchoolId == schoolId
                && lesson.TeacherProfileId == teacherProfileId
                && link.IsActive
                && user.IsActive
            orderby lesson.LessonDateUtc descending
            select new TeacherLessonListItem(
                lesson.Id,
                lesson.StudentProfileId,
                user.DisplayName,
                student.Instrument,
                lesson.Title,
                lesson.LessonDateUtc,
                lesson.CompletedAtUtc,
                lesson.Status,
                lesson.Notes))
            .Skip(safeSkip)
            .Take(safeTake)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<LessonDetailSummary> GetLessonDetailAsync(
        AuthenticatedUserProfile profile,
        Guid studentProfileId,
        Guid lessonId,
        CancellationToken cancellationToken = default)
    {
        var (schoolId, teacherProfileId) = LearningAuthorization.RequireTeacher(profile);
        await LearningAuthorization.EnsureTeacherCanAccessStudentAsync(
            _dbContext,
            schoolId,
            teacherProfileId,
            studentProfileId,
            cancellationToken);

        var lesson = await _dbContext.Lessons
            .AsNoTracking()
            .Where(item => item.Id == lessonId
                && item.SchoolId == schoolId
                && item.TeacherProfileId == teacherProfileId
                && item.StudentProfileId == studentProfileId)
            .Select(item => new LessonSummary(
                item.Id,
                item.StudentProfileId,
                item.Title,
                item.LessonDateUtc,
                item.CompletedAtUtc,
                item.Status,
                item.Notes))
            .FirstOrDefaultAsync(cancellationToken);

        if (lesson is null)
        {
            throw new KeyNotFoundException("Lesson was not found.");
        }

        var student = await (
            from link in _dbContext.TeacherStudents.AsNoTracking()
            join profileRow in _dbContext.StudentProfiles.AsNoTracking() on link.StudentProfileId equals profileRow.Id
            join user in _dbContext.SchoolUsers.AsNoTracking() on profileRow.SchoolUserId equals user.Id
            where link.SchoolId == schoolId
                && link.TeacherProfileId == teacherProfileId
                && link.StudentProfileId == studentProfileId
                && link.IsActive
            select new TeacherStudentSummary(
                profileRow.Id,
                user.Id,
                user.DisplayName,
                user.Email,
                profileRow.Instrument,
                profileRow.Level,
                profileRow.Notes,
                profileRow.CurrentXp,
                profileRow.CurrentLevel,
                0,
                null,
                null))
            .FirstAsync(cancellationToken);

        return new LessonDetailSummary(lesson, student);
    }

    public async Task<LessonSummary> UpdateLessonAsync(
        AuthenticatedUserProfile profile,
        UpdateLessonRequest request,
        CancellationToken cancellationToken = default)
    {
        var (schoolId, teacherProfileId) = LearningAuthorization.RequireTeacher(profile);
        await LearningAuthorization.EnsureTeacherCanAccessStudentAsync(
            _dbContext,
            schoolId,
            teacherProfileId,
            request.StudentProfileId,
            cancellationToken);

        var lesson = await _dbContext.Lessons.FirstOrDefaultAsync(
            item => item.Id == request.LessonId
                && item.SchoolId == schoolId
                && item.TeacherProfileId == teacherProfileId
                && item.StudentProfileId == request.StudentProfileId,
            cancellationToken)
            ?? throw new KeyNotFoundException("Lesson was not found.");

        lesson.UpdateDetails(request.Title, request.LessonDateUtc, request.Notes, DateTime.UtcNow);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToSummary(lesson);
    }

    public async Task DeleteLessonAsync(
        AuthenticatedUserProfile profile,
        Guid studentProfileId,
        Guid lessonId,
        CancellationToken cancellationToken = default)
    {
        var (schoolId, teacherProfileId) = LearningAuthorization.RequireTeacher(profile);
        await LearningAuthorization.EnsureTeacherCanAccessStudentAsync(
            _dbContext,
            schoolId,
            teacherProfileId,
            studentProfileId,
            cancellationToken);

        var lesson = await _dbContext.Lessons.FirstOrDefaultAsync(
            item => item.Id == lessonId
                && item.SchoolId == schoolId
                && item.TeacherProfileId == teacherProfileId
                && item.StudentProfileId == studentProfileId,
            cancellationToken)
            ?? throw new KeyNotFoundException("Aula nao encontrada.");

        _dbContext.Lessons.Remove(lesson);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CompleteLessonAsync(
        AuthenticatedUserProfile profile,
        Guid lessonId,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var (schoolId, teacherProfileId) = LearningAuthorization.RequireTeacher(profile);
        var lesson = await _dbContext.Lessons.FirstOrDefaultAsync(
            item => item.Id == lessonId && item.SchoolId == schoolId && item.TeacherProfileId == teacherProfileId,
            cancellationToken);

        if (lesson is null)
        {
            throw new KeyNotFoundException("Lesson was not found.");
        }

        await LearningAuthorization.EnsureTeacherCanAccessStudentAsync(
            _dbContext,
            schoolId,
            teacherProfileId,
            lesson.StudentProfileId,
            cancellationToken);

        lesson.Complete(notes, DateTime.UtcNow);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static LessonSummary ToSummary(Lesson lesson)
    {
        return new LessonSummary(
            lesson.Id,
            lesson.StudentProfileId,
            lesson.Title,
            lesson.LessonDateUtc,
            lesson.CompletedAtUtc,
            lesson.Status,
            lesson.Notes);
    }
}
