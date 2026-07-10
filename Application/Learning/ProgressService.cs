using Microsoft.EntityFrameworkCore;
using YourRhythmStudio.Application.Users;
using YourRhythmStudio.Domain.Learning.Enums;
using YourRhythmStudio.Infrastructure.Data;

namespace YourRhythmStudio.Application.Learning;

public sealed class ProgressService
{
    private readonly YourRhythmDbContext _dbContext;

    public ProgressService(YourRhythmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ProgressSummary> GetCurrentStudentProgressAsync(
        AuthenticatedUserProfile profile,
        CancellationToken cancellationToken = default)
    {
        var (schoolId, studentProfileId) = LearningAuthorization.RequireStudent(profile);
        return await GetStudentProgressAsync(schoolId, studentProfileId, cancellationToken);
    }

    public async Task<ProgressSummary> GetTeacherStudentProgressAsync(
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

        return await GetStudentProgressAsync(schoolId, studentProfileId, cancellationToken);
    }

    public async Task<StudentDashboardSummary> GetStudentDashboardAsync(
        AuthenticatedUserProfile profile,
        CancellationToken cancellationToken = default)
    {
        var (schoolId, studentProfileId) = LearningAuthorization.RequireStudent(profile);
        var progress = await GetStudentProgressAsync(schoolId, studentProfileId, cancellationToken);

        var pending = await _dbContext.Assignments
            .AsNoTracking()
            .Where(assignment => assignment.SchoolId == schoolId
                && assignment.StudentProfileId == studentProfileId
                && assignment.Status != AssignmentStatus.Completed
                && assignment.Status != AssignmentStatus.Skipped)
            .OrderBy(assignment => assignment.DueAtUtc ?? DateTime.MaxValue)
            .Take(5)
            .Select(assignment => new AssignmentSummary(
                assignment.Id,
                assignment.Title,
                assignment.Description,
                assignment.DueAtUtc,
                assignment.Status,
                assignment.CompletedAtUtc,
                assignment.XpReward,
                assignment.XpGranted,
                assignment.Rarity))
            .ToArrayAsync(cancellationToken);

        var completed = await _dbContext.Assignments
            .AsNoTracking()
            .Where(assignment => assignment.SchoolId == schoolId
                && assignment.StudentProfileId == studentProfileId
                && assignment.Status == AssignmentStatus.Completed)
            .OrderByDescending(assignment => assignment.CompletedAtUtc)
            .Take(5)
            .Select(assignment => new AssignmentSummary(
                assignment.Id,
                assignment.Title,
                assignment.Description,
                assignment.DueAtUtc,
                assignment.Status,
                assignment.CompletedAtUtc,
                assignment.XpReward,
                assignment.XpGranted,
                assignment.Rarity))
            .ToArrayAsync(cancellationToken);

        var repertoire = await _dbContext.RepertoireItems
            .AsNoTracking()
            .Where(item => item.SchoolId == schoolId
                && item.StudentProfileId == studentProfileId
                && item.Status != RepertoireStatus.Archived)
            .OrderByDescending(item => item.UpdatedAtUtc)
            .Take(5)
            .Select(item => new RepertoireSummary(
                item.Id,
                item.Title,
                item.Status,
                item.ProgressPercent,
                item.Notes,
                item.ReferenceUrl,
                item.AudioOriginalFileName,
                item.AudioContentType,
                item.AudioSizeBytes,
                item.AudioStoredFileName != null))
            .ToArrayAsync(cancellationToken);

        var feedback = await _dbContext.FeedbackEntries
            .AsNoTracking()
            .Where(entry => entry.SchoolId == schoolId
                && entry.StudentProfileId == studentProfileId
                && entry.VisibleToStudent)
            .OrderByDescending(entry => entry.CreatedAtUtc)
            .Take(5)
            .Select(entry => new FeedbackSummary(entry.Id, entry.Message, entry.VisibleToStudent, entry.CreatedAtUtc))
            .ToArrayAsync(cancellationToken);

        return new StudentDashboardSummary(progress, pending, completed, repertoire, feedback);
    }

    private async Task<ProgressSummary> GetStudentProgressAsync(
        Guid schoolId,
        Guid studentProfileId,
        CancellationToken cancellationToken)
    {
        var student = await _dbContext.StudentProfiles
            .AsNoTracking()
            .FirstAsync(student => student.SchoolId == schoolId && student.Id == studentProfileId, cancellationToken);

        var pending = await _dbContext.Assignments.CountAsync(
            assignment => assignment.SchoolId == schoolId
                && assignment.StudentProfileId == studentProfileId
                && assignment.Status != AssignmentStatus.Completed
                && assignment.Status != AssignmentStatus.Skipped,
            cancellationToken);

        var completed = await _dbContext.Assignments.CountAsync(
            assignment => assignment.SchoolId == schoolId
                && assignment.StudentProfileId == studentProfileId
                && assignment.Status == AssignmentStatus.Completed,
            cancellationToken);

        var repertoireInProgress = await _dbContext.RepertoireItems.CountAsync(
            item => item.SchoolId == schoolId
                && item.StudentProfileId == studentProfileId
                && item.Status == RepertoireStatus.Practicing,
            cancellationToken);

        var recentXp = await _dbContext.XpEvents
            .AsNoTracking()
            .Where(item => item.SchoolId == schoolId && item.StudentProfileId == studentProfileId)
            .OrderByDescending(item => item.CreatedAtUtc)
            .Take(8)
            .Select(item => new XpEventSummary(item.Id, item.Points, item.Description, item.CreatedAtUtc))
            .ToArrayAsync(cancellationToken);

        var levelProgress = LearningLevelCalculator.CalculateProgress(student.CurrentXp, student.CurrentLevel);

        return new ProgressSummary(
            student.CurrentXp,
            student.CurrentLevel,
            levelProgress.CurrentLevel.Name,
            levelProgress.CurrentLevel.MinXp,
            levelProgress.CurrentLevel.MaxXp,
            levelProgress.XpInCurrentRange,
            levelProgress.XpRequiredInCurrentRange,
            levelProgress.Percent,
            levelProgress.AwaitingPromotion,
            levelProgress.NextLevel?.Name,
            pending,
            completed,
            repertoireInProgress,
            recentXp);
    }
}
