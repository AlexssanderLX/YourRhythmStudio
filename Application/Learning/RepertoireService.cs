using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using YourRhythmStudio.Application.Users;
using YourRhythmStudio.Domain.Learning;
using YourRhythmStudio.Domain.Learning.Enums;
using YourRhythmStudio.Infrastructure.Data;

namespace YourRhythmStudio.Application.Learning;

public sealed class RepertoireService
{
    private const long MaxAudioBytes = 30 * 1024 * 1024;
    private const long MaxMaterialBytes = 5 * 1024 * 1024;
    private const int MaxMaterialsPerItem = 10;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".wav", ".m4a", ".aac", ".ogg", ".oga", ".flac", ".webm"
    };

    private static readonly HashSet<string> AllowedMaterialExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp",
        ".pdf",
        ".mp3", ".wav", ".m4a", ".aac", ".ogg", ".oga", ".flac",
        ".mp4", ".mov", ".webm"
    };

    private static readonly HashSet<string> DangerousExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".dll", ".bat", ".cmd", ".sh", ".ps1", ".msi", ".app",
        ".php", ".asp", ".aspx", ".jsp", ".py", ".rb", ".js", ".ts",
        ".jar", ".com", ".pif", ".scr", ".vbs", ".vb", ".htaccess"
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

        bool hasContent = !string.IsNullOrWhiteSpace(request.ReferenceUrl)
            || request.Audio is not null
            || !string.IsNullOrWhiteSpace(request.Notes);
        if (!hasContent)
            throw new ArgumentException("Informe ao menos um conteudo: descricao, link ou audio.");

        var now = DateTime.UtcNow;
        var referenceUrl = NormalizeReferenceUrl(request.ReferenceUrl);
        var item = new RepertoireItem(
            schoolId,
            teacherProfileId,
            request.StudentProfileId,
            request.Title,
            now);

        item.UpdateDetails(request.Title, request.Notes, referenceUrl, now, request.ComposerName, request.InstrumentName);

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

        bool hasContentInRequest = !string.IsNullOrWhiteSpace(request.ReferenceUrl)
            || request.Audio is not null
            || !string.IsNullOrWhiteSpace(request.Notes);

        if (!hasContentInRequest)
        {
            var existingHasAudio = await _dbContext.RepertoireItems.AnyAsync(
                i => i.Id == request.RepertoireItemId
                    && i.SchoolId == schoolId
                    && i.TeacherProfileId == teacherProfileId
                    && i.StudentProfileId == request.StudentProfileId
                    && i.AudioStoredFileName != null,
                cancellationToken);

            if (!existingHasAudio)
            {
                var existingHasMaterials = await _dbContext.RepertoireItemMaterials.AnyAsync(
                    m => m.RepertoireItemId == request.RepertoireItemId && m.SchoolId == schoolId,
                    cancellationToken);
                if (!existingHasMaterials)
                    throw new ArgumentException("O repertorio precisa de ao menos um conteudo: descricao, link ou arquivo.");
            }
        }

        var item = await _dbContext.RepertoireItems.FirstOrDefaultAsync(
            entry => entry.Id == request.RepertoireItemId
                && entry.SchoolId == schoolId
                && entry.TeacherProfileId == teacherProfileId
                && entry.StudentProfileId == request.StudentProfileId,
            cancellationToken)
            ?? throw new KeyNotFoundException("Repertoire item was not found.");

        var now = DateTime.UtcNow;
        item.UpdateDetails(request.Title, request.Notes, NormalizeReferenceUrl(request.ReferenceUrl), now, request.ComposerName, request.InstrumentName);

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

    public async Task<RepertoireTeacherDetail?> GetDetailForTeacherAsync(
        AuthenticatedUserProfile profile,
        Guid studentProfileId,
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        var (schoolId, teacherProfileId) = LearningAuthorization.RequireTeacher(profile);
        await LearningAuthorization.EnsureTeacherCanAccessStudentAsync(
            _dbContext, schoolId, teacherProfileId, studentProfileId, cancellationToken);

        var item = await _dbContext.RepertoireItems
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == itemId
                && i.SchoolId == schoolId
                && i.TeacherProfileId == teacherProfileId
                && i.StudentProfileId == studentProfileId,
                cancellationToken);

        if (item is null) return null;

        var materials = await _dbContext.RepertoireItemMaterials
            .AsNoTracking()
            .Where(m => m.RepertoireItemId == itemId && m.SchoolId == schoolId)
            .OrderBy(m => m.Order)
            .ThenBy(m => m.AddedAtUtc)
            .Select(m => new RepertoireMaterialSummary(
                m.Id,
                m.MaterialType,
                m.Title,
                m.OriginalFileName,
                m.ContentType,
                m.SizeBytes,
                m.AddedAtUtc))
            .ToArrayAsync(cancellationToken);

        return new RepertoireTeacherDetail(
            item.Id,
            item.Title,
            item.Status,
            item.ProgressPercent,
            item.Notes,
            item.ComposerName,
            item.InstrumentName,
            item.ReferenceUrl,
            item.AudioOriginalFileName,
            item.AudioContentType,
            item.AudioSizeBytes,
            item.AudioStoredFileName is not null,
            materials,
            item.CreatedAtUtc,
            item.UpdatedAtUtc);
    }

    public async Task ArchiveRepertoireAsync(
        AuthenticatedUserProfile profile,
        Guid studentProfileId,
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        var (schoolId, teacherProfileId) = LearningAuthorization.RequireTeacher(profile);
        await LearningAuthorization.EnsureTeacherCanAccessStudentAsync(
            _dbContext, schoolId, teacherProfileId, studentProfileId, cancellationToken);

        var item = await _dbContext.RepertoireItems
            .FirstOrDefaultAsync(i => i.Id == itemId
                && i.SchoolId == schoolId
                && i.TeacherProfileId == teacherProfileId
                && i.StudentProfileId == studentProfileId,
                cancellationToken)
            ?? throw new KeyNotFoundException("Repertoire item was not found.");

        if (item.Status == RepertoireStatus.Archived)
            return;

        var materials = await _dbContext.RepertoireItemMaterials
            .Where(m => m.RepertoireItemId == itemId && m.SchoolId == schoolId)
            .ToListAsync(cancellationToken);

        var legacyAudioPath = item.AudioStoredFileName is not null
            ? Path.Combine(GetStorageRoot(), Path.GetFileName(item.AudioStoredFileName))
            : null;

        var materialPaths = materials
            .Where(m => m.StoredFileName is not null)
            .Select(m => Path.Combine(GetMaterialStorageRoot(), Path.GetFileName(m.StoredFileName!)))
            .ToList();

        var now = DateTime.UtcNow;
        item.Archive(now);
        item.ClearFiles(now);

        _dbContext.RepertoireItemMaterials.RemoveRange(materials);
        await _dbContext.SaveChangesAsync(cancellationToken);

        DeleteFileIfExists(legacyAudioPath);
        foreach (var path in materialPaths)
            DeleteFileIfExists(path);
    }

    public async Task<RepertoireMaterialSummary> AddMaterialAsync(
        AuthenticatedUserProfile profile,
        Guid studentProfileId,
        Guid itemId,
        string fileName,
        string contentType,
        long length,
        Func<Stream> openReadStream,
        CancellationToken cancellationToken = default)
    {
        var (schoolId, teacherProfileId) = LearningAuthorization.RequireTeacher(profile);
        await LearningAuthorization.EnsureTeacherCanAccessStudentAsync(
            _dbContext, schoolId, teacherProfileId, studentProfileId, cancellationToken);

        var item = await _dbContext.RepertoireItems
            .FirstOrDefaultAsync(i => i.Id == itemId
                && i.SchoolId == schoolId
                && i.TeacherProfileId == teacherProfileId
                && i.StudentProfileId == studentProfileId
                && i.Status != RepertoireStatus.Archived,
                cancellationToken)
            ?? throw new KeyNotFoundException("Repertoire item was not found.");

        var existingCount = await _dbContext.RepertoireItemMaterials
            .CountAsync(m => m.RepertoireItemId == itemId && m.SchoolId == schoolId, cancellationToken);

        if (existingCount >= MaxMaterialsPerItem)
            throw new InvalidOperationException($"Limite de {MaxMaterialsPerItem} arquivos por repertorio atingido.");

        var (storedFileName, materialType) = await SaveMaterialAsync(fileName, contentType, length, openReadStream, cancellationToken);

        var originalFileName = Path.GetFileName(fileName);
        var material = new RepertoireItemMaterial(itemId, schoolId, materialType, originalFileName, existingCount + 1)
        {
            StoredFileName = storedFileName,
            OriginalFileName = originalFileName,
            ContentType = contentType,
            SizeBytes = length
        };

        _dbContext.RepertoireItemMaterials.Add(material);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new RepertoireMaterialSummary(
            material.Id,
            material.MaterialType,
            material.Title,
            material.OriginalFileName,
            material.ContentType,
            material.SizeBytes,
            material.AddedAtUtc);
    }

    public async Task RemoveMaterialAsync(
        AuthenticatedUserProfile profile,
        Guid studentProfileId,
        Guid materialId,
        CancellationToken cancellationToken = default)
    {
        var (schoolId, teacherProfileId) = LearningAuthorization.RequireTeacher(profile);

        var material = await _dbContext.RepertoireItemMaterials
            .FirstOrDefaultAsync(m => m.Id == materialId && m.SchoolId == schoolId, cancellationToken)
            ?? throw new KeyNotFoundException("Material nao encontrado.");

        var item = await _dbContext.RepertoireItems
            .FirstOrDefaultAsync(i => i.Id == material.RepertoireItemId
                && i.SchoolId == schoolId
                && i.TeacherProfileId == teacherProfileId
                && i.StudentProfileId == studentProfileId,
                cancellationToken)
            ?? throw new UnauthorizedAccessException("Sem permissao para remover este arquivo.");

        var remainingMaterialCount = await _dbContext.RepertoireItemMaterials
            .CountAsync(m => m.RepertoireItemId == material.RepertoireItemId
                && m.SchoolId == schoolId
                && m.Id != materialId,
                cancellationToken);

        bool wouldBeEmpty = string.IsNullOrWhiteSpace(item.Notes)
            && string.IsNullOrWhiteSpace(item.ReferenceUrl)
            && item.AudioStoredFileName is null
            && remainingMaterialCount == 0;

        if (wouldBeEmpty)
            throw new InvalidOperationException("Nao e possivel remover: o repertorio ficaria sem conteudo util.");

        var physicalPath = material.StoredFileName is not null
            ? Path.Combine(GetMaterialStorageRoot(), Path.GetFileName(material.StoredFileName))
            : null;

        _dbContext.RepertoireItemMaterials.Remove(material);
        await _dbContext.SaveChangesAsync(cancellationToken);

        DeleteFileIfExists(physicalPath);
    }

    public async Task<RepertoireAudioFile?> GetMaterialForTeacherAsync(
        AuthenticatedUserProfile profile,
        Guid studentProfileId,
        Guid itemId,
        Guid materialId,
        CancellationToken cancellationToken = default)
    {
        var (schoolId, teacherProfileId) = LearningAuthorization.RequireTeacher(profile);
        await LearningAuthorization.EnsureTeacherCanAccessStudentAsync(
            _dbContext, schoolId, teacherProfileId, studentProfileId, cancellationToken);

        var material = await _dbContext.RepertoireItemMaterials
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == materialId
                && m.SchoolId == schoolId
                && m.RepertoireItemId == itemId,
                cancellationToken);

        if (material?.StoredFileName is null || material.OriginalFileName is null || material.ContentType is null)
            return null;

        var itemBelongs = await _dbContext.RepertoireItems
            .AnyAsync(i => i.Id == itemId
                && i.SchoolId == schoolId
                && i.TeacherProfileId == teacherProfileId
                && i.StudentProfileId == studentProfileId,
                cancellationToken);

        if (!itemBelongs) return null;

        var fileName = Path.GetFileName(material.StoredFileName);
        var physicalPath = Path.Combine(GetMaterialStorageRoot(), fileName);
        if (!File.Exists(physicalPath)) return null;

        return new RepertoireAudioFile(physicalPath, material.ContentType, material.OriginalFileName);
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

    private string GetMaterialStorageRoot()
    {
        return Path.Combine(_environment.ContentRootPath, "storage", "uploads", "repertoire-materials");
    }

    private async Task<(string StoredFileName, RepertoireMaterialType MaterialType)> SaveMaterialAsync(
        string fileName, string contentType, long length, Func<Stream> openReadStream, CancellationToken cancellationToken)
    {
        if (length <= 0)
            throw new ArgumentException("Arquivo vazio.");

        if (length > MaxMaterialBytes)
            throw new ArgumentException("Arquivo maior que 5 MB.");

        var originalFileName = Path.GetFileName(fileName);
        var extension = Path.GetExtension(originalFileName);

        if (string.IsNullOrWhiteSpace(extension))
            throw new ArgumentException("Formato de arquivo nao permitido.");

        if (DangerousExtensions.Contains(extension))
            throw new ArgumentException("Formato de arquivo nao permitido.");

        // Block double extensions like "script.php.jpg"
        var nameWithoutLastExt = Path.GetFileNameWithoutExtension(originalFileName);
        if (DangerousExtensions.Contains(Path.GetExtension(nameWithoutLastExt)))
            throw new ArgumentException("Formato de arquivo nao permitido.");

        if (!AllowedMaterialExtensions.Contains(extension))
            throw new ArgumentException("Formato de arquivo nao permitido.");

        var materialType = DetermineMaterialType(extension);

        if (!IsAllowedMaterialContentType(contentType, extension, materialType))
            throw new ArgumentException("Tipo de arquivo nao permitido.");

        var storedFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var storageRoot = GetMaterialStorageRoot();
        Directory.CreateDirectory(storageRoot);

        var physicalPath = Path.Combine(storageRoot, storedFileName);
        await using var source = openReadStream();
        await using var destination = File.Create(physicalPath);
        await source.CopyToAsync(destination, cancellationToken);

        return (storedFileName, materialType);
    }

    private static RepertoireMaterialType DetermineMaterialType(string extension) =>
        extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" => RepertoireMaterialType.Image,
            ".mp3" or ".wav" or ".m4a" or ".aac" or ".ogg" or ".oga" or ".flac" => RepertoireMaterialType.Audio,
            ".mp4" or ".mov" or ".webm" => RepertoireMaterialType.Video,
            _ => RepertoireMaterialType.Pdf
        };

    private static bool IsAllowedMaterialContentType(string contentType, string extension, RepertoireMaterialType type) =>
        type switch
        {
            RepertoireMaterialType.Image => contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase),
            RepertoireMaterialType.Audio => contentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase)
                || (extension.Equals(".ogg", StringComparison.OrdinalIgnoreCase)
                    && contentType.Equals("application/ogg", StringComparison.OrdinalIgnoreCase)),
            RepertoireMaterialType.Video => contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase),
            RepertoireMaterialType.Pdf => contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase),
            _ => false
        };

    private static void DeleteFileIfExists(string? path)
    {
        if (path is null) return;
        try { if (File.Exists(path)) File.Delete(path); }
        catch { /* physical cleanup is best-effort */ }
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
