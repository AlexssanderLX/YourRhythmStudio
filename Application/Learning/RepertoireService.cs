using Microsoft.EntityFrameworkCore;
using YourRhythmStudio.Application.Users;
using YourRhythmStudio.Domain.Learning;
using YourRhythmStudio.Infrastructure.Data;

namespace YourRhythmStudio.Application.Learning;

public sealed class RepertoireService
{
    private readonly YourRhythmDbContext _dbContext;

    public RepertoireService(YourRhythmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RepertoireSummary> AddRepertoireAsync(
        AuthenticatedUserProfile profile,
        AddRepertoireRequest request,
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
        var item = new RepertoireItem(
            schoolId,
            teacherProfileId,
            request.StudentProfileId,
            request.Title,
            request.ComposerOrArtist,
            request.Instrument,
            request.Level,
            now);

        if (!string.IsNullOrWhiteSpace(request.Notes) || !string.IsNullOrWhiteSpace(request.ReferenceUrl))
        {
            item.UpdateDetails(
                request.Title,
                request.ComposerOrArtist,
                request.Instrument,
                request.Level,
                request.Notes,
                request.ReferenceUrl,
                now);
        }

        _dbContext.RepertoireItems.Add(item);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToSummary(item);
    }

    public async Task<IReadOnlyCollection<RepertoireSummary>> ListForTeacherStudentAsync(
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

        return await QueryRepertoire(schoolId, studentProfileId, teacherProfileId)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<RepertoireSummary>> ListForCurrentStudentAsync(
        AuthenticatedUserProfile profile,
        CancellationToken cancellationToken = default)
    {
        var (schoolId, studentProfileId) = LearningAuthorization.RequireStudent(profile);
        return await QueryRepertoire(schoolId, studentProfileId, null).ToArrayAsync(cancellationToken);
    }

    public async Task UpdateProgressAsync(
        AuthenticatedUserProfile profile,
        UpdateRepertoireProgressRequest request,
        CancellationToken cancellationToken = default)
    {
        var (schoolId, teacherProfileId) = LearningAuthorization.RequireTeacher(profile);
        var item = await _dbContext.RepertoireItems.FirstOrDefaultAsync(
            entry => entry.Id == request.RepertoireItemId
                && entry.SchoolId == schoolId
                && entry.TeacherProfileId == teacherProfileId,
            cancellationToken);

        if (item is null)
        {
            throw new KeyNotFoundException("Repertoire item was not found.");
        }

        await LearningAuthorization.EnsureTeacherCanAccessStudentAsync(
            _dbContext,
            schoolId,
            teacherProfileId,
            item.StudentProfileId,
            cancellationToken);

        item.UpdateProgress(request.ProgressPercent, DateTime.UtcNow);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<RepertoireSummary> QueryRepertoire(Guid schoolId, Guid studentProfileId, Guid? teacherProfileId)
    {
        var query = _dbContext.RepertoireItems
            .AsNoTracking()
            .Where(item => item.SchoolId == schoolId && item.StudentProfileId == studentProfileId);

        if (teacherProfileId is not null)
        {
            query = query.Where(item => item.TeacherProfileId == teacherProfileId.Value);
        }

        return query
            .OrderByDescending(item => item.UpdatedAtUtc)
            .Select(item => new RepertoireSummary(
                item.Id,
                item.Title,
                item.ComposerOrArtist,
                item.Instrument,
                item.Level,
                item.Status,
                item.ProgressPercent,
                item.Notes,
                item.ReferenceUrl));
    }

    private static RepertoireSummary ToSummary(RepertoireItem item)
    {
        return new RepertoireSummary(
            item.Id,
            item.Title,
            item.ComposerOrArtist,
            item.Instrument,
            item.Level,
            item.Status,
            item.ProgressPercent,
            item.Notes,
            item.ReferenceUrl);
    }
}
