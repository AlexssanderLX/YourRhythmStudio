using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using YourRhythmStudio.Application.Users;
using YourRhythmStudio.Domain.Learning;
using YourRhythmStudio.Infrastructure.Data;

namespace YourRhythmStudio.Application.Learning;

public sealed class RepertoireService
{
    private const long MaxAudioBytes = 30 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".wav", ".m4a", ".aac", ".ogg", ".oga", ".flac", ".webm"
    };

    private readonly YourRhythmDbContext _dbContext;
    private readonly IWebHostEnvironment _environment;

    public RepertoireService(YourRhythmDbContext dbContext, IWebHostEnvironment environment)
    {
        _dbContext = dbContext;
        _environment = environment;
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

        if (string.IsNullOrWhiteSpace(request.ReferenceUrl) && request.Audio is null)
        {
            throw new ArgumentException("Informe um link, um arquivo de audio ou os dois.");
        }

        var now = DateTime.UtcNow;
        var referenceUrl = NormalizeReferenceUrl(request.ReferenceUrl);
        var item = new RepertoireItem(
            schoolId,
            teacherProfileId,
            request.StudentProfileId,
            request.Title,
            now);

        item.UpdateDetails(request.Title, request.Notes, referenceUrl, now);

        if (request.Audio is not null)
        {
            var savedAudio = await SaveAudioAsync(request.Audio, cancellationToken);
            item.AttachAudio(
                savedAudio.StoredFileName,
                savedAudio.OriginalFileName,
                savedAudio.ContentType,
                savedAudio.SizeBytes,
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

    public async Task<RepertoireSummary> UpdateRepertoireAsync(
        AuthenticatedUserProfile profile,
        UpdateRepertoireRequest request,
        CancellationToken cancellationToken = default)
    {
        var (schoolId, teacherProfileId) = LearningAuthorization.RequireTeacher(profile);
        await LearningAuthorization.EnsureTeacherCanAccessStudentAsync(
            _dbContext,
            schoolId,
            teacherProfileId,
            request.StudentProfileId,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(request.ReferenceUrl) && request.Audio is null)
        {
            var hasExistingAudio = await _dbContext.RepertoireItems.AnyAsync(
                item => item.Id == request.RepertoireItemId
                    && item.SchoolId == schoolId
                    && item.TeacherProfileId == teacherProfileId
                    && item.StudentProfileId == request.StudentProfileId
                    && item.AudioStoredFileName != null,
                cancellationToken);
            if (!hasExistingAudio)
                throw new ArgumentException("Informe um link, um arquivo de audio ou os dois.");
        }

        var item = await _dbContext.RepertoireItems.FirstOrDefaultAsync(
            entry => entry.Id == request.RepertoireItemId
                && entry.SchoolId == schoolId
                && entry.TeacherProfileId == teacherProfileId
                && entry.StudentProfileId == request.StudentProfileId,
            cancellationToken)
            ?? throw new KeyNotFoundException("Repertoire item was not found.");

        var now = DateTime.UtcNow;
        item.UpdateDetails(request.Title, request.Notes, NormalizeReferenceUrl(request.ReferenceUrl), now);

        if (request.Audio is not null)
        {
            var savedAudio = await SaveAudioAsync(request.Audio, cancellationToken);
            item.AttachAudio(
                savedAudio.StoredFileName,
                savedAudio.OriginalFileName,
                savedAudio.ContentType,
                savedAudio.SizeBytes,
                now);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToSummary(item);
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

    public async Task<RepertoireAudioFile?> GetAudioForCurrentStudentAsync(
        AuthenticatedUserProfile profile,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (schoolId, studentProfileId) = LearningAuthorization.RequireStudent(profile);
        var item = await _dbContext.RepertoireItems
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.SchoolId == schoolId
                && r.StudentProfileId == studentProfileId
                && r.Id == id,
                cancellationToken);

        return ToAudioFile(item);
    }

    public async Task<RepertoireAudioFile?> GetAudioForTeacherAsync(
        AuthenticatedUserProfile profile,
        Guid studentProfileId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (schoolId, teacherProfileId) = LearningAuthorization.RequireTeacher(profile);
        await LearningAuthorization.EnsureTeacherCanAccessStudentAsync(
            _dbContext,
            schoolId,
            teacherProfileId,
            studentProfileId,
            cancellationToken);

        var item = await _dbContext.RepertoireItems
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.SchoolId == schoolId
                && r.TeacherProfileId == teacherProfileId
                && r.StudentProfileId == studentProfileId
                && r.Id == id,
                cancellationToken);

        return ToAudioFile(item);
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
                item.Status,
                item.ProgressPercent,
                item.Notes,
                item.ReferenceUrl,
                item.AudioOriginalFileName,
                item.AudioContentType,
                item.AudioSizeBytes,
                item.AudioStoredFileName != null,
                item.CreatedAtUtc));
    }

    private static RepertoireSummary ToSummary(RepertoireItem item)
    {
        return new RepertoireSummary(
            item.Id,
            item.Title,
            item.Status,
            item.ProgressPercent,
            item.Notes,
            item.ReferenceUrl,
            item.AudioOriginalFileName,
            item.AudioContentType,
            item.AudioSizeBytes,
            item.AudioStoredFileName != null,
            item.CreatedAtUtc);
    }

    private async Task<SavedAudio> SaveAudioAsync(RepertoireAudioUpload upload, CancellationToken cancellationToken)
    {
        if (upload.Length <= 0)
        {
            throw new ArgumentException("Arquivo de audio vazio.");
        }

        if (upload.Length > MaxAudioBytes)
        {
            throw new ArgumentException("Arquivo de audio maior que 30 MB.");
        }

        var originalFileName = Path.GetFileName(upload.FileName);
        var extension = Path.GetExtension(originalFileName);
        if (!AllowedExtensions.Contains(extension))
        {
            throw new ArgumentException("Formato de audio nao permitido.");
        }

        if (!IsAllowedAudioContentType(upload.ContentType, extension))
        {
            throw new ArgumentException("Tipo de arquivo de audio nao permitido.");
        }

        var storedFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var storageRoot = GetStorageRoot();
        Directory.CreateDirectory(storageRoot);

        var physicalPath = Path.Combine(storageRoot, storedFileName);
        await using var source = upload.OpenReadStream();
        await using var destination = File.Create(physicalPath);
        await source.CopyToAsync(destination, cancellationToken);

        return new SavedAudio(storedFileName, originalFileName, upload.ContentType, upload.Length);
    }

    private RepertoireAudioFile? ToAudioFile(RepertoireItem? item)
    {
        if (item?.AudioStoredFileName is null
            || item.AudioOriginalFileName is null
            || item.AudioContentType is null)
        {
            return null;
        }

        var fileName = Path.GetFileName(item.AudioStoredFileName);
        var physicalPath = Path.Combine(GetStorageRoot(), fileName);
        if (!File.Exists(physicalPath))
        {
            return null;
        }

        return new RepertoireAudioFile(physicalPath, item.AudioContentType, item.AudioOriginalFileName);
    }

    private string GetStorageRoot()
    {
        return Path.Combine(_environment.ContentRootPath, "storage", "uploads", "repertoire-audio");
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

    private static bool IsAllowedAudioContentType(string contentType, string extension)
    {
        if (contentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return extension.Equals(".ogg", StringComparison.OrdinalIgnoreCase)
            && contentType.Equals("application/ogg", StringComparison.OrdinalIgnoreCase);
    }

    private sealed record SavedAudio(
        string StoredFileName,
        string OriginalFileName,
        string ContentType,
        long SizeBytes);
}
