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
        var referenceUrl = NormalizeReferenceUrl(request.ReferenceUrl);
        var item = new RepertoireItem(
            schoolId,
            teacherProfileId,
            request.StudentProfileId,
            request.Title,
            request.ComposerOrArtist,
            request.Instrument,
            request.Level,
            now);

        if (!string.IsNullOrWhiteSpace(request.Notes) || !string.IsNullOrWhiteSpace(referenceUrl))
        {
            item.UpdateDetails(
                request.Title,
                request.ComposerOrArtist,
                request.Instrument,
                request.Level,
                request.Notes,
                referenceUrl,
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

    public async Task<RepertoireSummary?> GetByIdForCurrentStudentAsync(
        AuthenticatedUserProfile profile,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (schoolId, studentProfileId) = LearningAuthorization.RequireStudent(profile);
        var item = await _dbContext.RepertoireItems
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.SchoolId == schoolId
                && r.StudentProfileId == studentProfileId
                && r.Id == id, cancellationToken);
        return item is null ? null : ToSummary(item);
    }

    public async Task<string?> OpenReferenceForCurrentStudentAsync(
        AuthenticatedUserProfile profile,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (schoolId, studentProfileId) = LearningAuthorization.RequireStudent(profile);
        var item = await _dbContext.RepertoireItems.FirstOrDefaultAsync(
            repertoire => repertoire.SchoolId == schoolId
                && repertoire.StudentProfileId == studentProfileId
                && repertoire.Id == id,
            cancellationToken);

        if (item is null)
        {
            return null;
        }

        if (item.ProgressPercent < 50)
        {
            item.UpdateProgress(50, DateTime.UtcNow);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return IsSafeReferenceUrl(item.ReferenceUrl) ? item.ReferenceUrl : null;
    }

    public async Task<bool> CompleteForCurrentStudentAsync(
        AuthenticatedUserProfile profile,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (schoolId, studentProfileId) = LearningAuthorization.RequireStudent(profile);
        var item = await _dbContext.RepertoireItems.FirstOrDefaultAsync(
            repertoire => repertoire.SchoolId == schoolId
                && repertoire.StudentProfileId == studentProfileId
                && repertoire.Id == id,
            cancellationToken);

        if (item is null)
        {
            return false;
        }

        item.UpdateProgress(100, DateTime.UtcNow);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
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

    private static string? NormalizeReferenceUrl(string? referenceUrl)
    {
        if (string.IsNullOrWhiteSpace(referenceUrl))
        {
            return null;
        }

        var trimmed = referenceUrl.Trim();
        if (!IsSafeReferenceUrl(trimmed))
        {
            throw new ArgumentException("Reference URL must use http or https.", nameof(referenceUrl));
        }

        return trimmed;
    }

    private static bool IsSafeReferenceUrl(string? referenceUrl)
    {
        return Uri.TryCreate(referenceUrl, UriKind.Absolute, out var uri)
            && uri.Scheme is "http" or "https";
    }
}
