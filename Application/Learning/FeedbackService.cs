using Microsoft.EntityFrameworkCore;
using YourRhythmStudio.Application.Users;
using YourRhythmStudio.Domain.Learning;
using YourRhythmStudio.Infrastructure.Data;

namespace YourRhythmStudio.Application.Learning;

public sealed class FeedbackService
{
    private readonly YourRhythmDbContext _dbContext;

    public FeedbackService(YourRhythmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<FeedbackSummary> CreateFeedbackAsync(
        AuthenticatedUserProfile profile,
        CreateFeedbackRequest request,
        CancellationToken cancellationToken = default)
    {
        var (schoolId, teacherProfileId) = LearningAuthorization.RequireTeacher(profile);
        await LearningAuthorization.EnsureTeacherCanAccessStudentAsync(
            _dbContext,
            schoolId,
            teacherProfileId,
            request.StudentProfileId,
            cancellationToken);

        var feedback = new FeedbackEntry(
            schoolId,
            teacherProfileId,
            request.StudentProfileId,
            request.Message,
            request.VisibleToStudent,
            DateTime.UtcNow);

        _dbContext.FeedbackEntries.Add(feedback);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new FeedbackSummary(feedback.Id, feedback.Message, feedback.VisibleToStudent, feedback.CreatedAtUtc);
    }

    public async Task<IReadOnlyCollection<FeedbackSummary>> ListVisibleForCurrentStudentAsync(
        AuthenticatedUserProfile profile,
        CancellationToken cancellationToken = default)
    {
        var (schoolId, studentProfileId) = LearningAuthorization.RequireStudent(profile);

        return await _dbContext.FeedbackEntries
            .AsNoTracking()
            .Where(entry => entry.SchoolId == schoolId
                && entry.StudentProfileId == studentProfileId
                && entry.VisibleToStudent)
            .OrderByDescending(entry => entry.CreatedAtUtc)
            .Select(entry => new FeedbackSummary(entry.Id, entry.Message, entry.VisibleToStudent, entry.CreatedAtUtc))
            .ToArrayAsync(cancellationToken);
    }
}

