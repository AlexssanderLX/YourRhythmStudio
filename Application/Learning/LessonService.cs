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

        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            lesson.UpdateDetails(request.Title, request.LessonDateUtc, request.Notes, now);
        }

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
                lesson.Title,
                lesson.LessonDateUtc,
                lesson.CompletedAtUtc,
                lesson.Status,
                lesson.Notes))
            .ToArrayAsync(cancellationToken);
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
            lesson.Title,
            lesson.LessonDateUtc,
            lesson.CompletedAtUtc,
            lesson.Status,
            lesson.Notes);
    }
}

