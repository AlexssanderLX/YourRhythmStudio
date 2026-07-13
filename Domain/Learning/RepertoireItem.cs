using YourRhythmStudio.Domain.Learning.Enums;

namespace YourRhythmStudio.Domain.Learning;

public sealed class RepertoireItem
{
    private RepertoireItem()
    {
    }

    public RepertoireItem(
        Guid schoolId,
        Guid teacherProfileId,
        Guid studentProfileId,
        string title,
        DateTime utcNow)
    {
        if (schoolId == Guid.Empty)
            throw new ArgumentException("SchoolId is required.", nameof(schoolId));

        if (teacherProfileId == Guid.Empty)
            throw new ArgumentException("TeacherProfileId is required.", nameof(teacherProfileId));

        if (studentProfileId == Guid.Empty)
            throw new ArgumentException("StudentProfileId is required.", nameof(studentProfileId));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Repertoire title is required.", nameof(title));

        Id = Guid.NewGuid();
        SchoolId = schoolId;
        TeacherProfileId = teacherProfileId;
        StudentProfileId = studentProfileId;
        Title = title.Trim();
        Status = RepertoireStatus.NotStarted;
        ProgressPercent = 0;
        CreatedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public Guid Id { get; private set; }

    public Guid SchoolId { get; private set; }

    public Guid TeacherProfileId { get; private set; }

    public Guid StudentProfileId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public RepertoireStatus Status { get; private set; }

    public int ProgressPercent { get; private set; }

    public string? Notes { get; private set; }

    public string? ComposerName { get; private set; }

    public string? InstrumentName { get; private set; }

    public string? ReferenceUrl { get; private set; }

    public string? AudioStoredFileName { get; private set; }

    public string? AudioOriginalFileName { get; private set; }

    public string? AudioContentType { get; private set; }

    public long? AudioSizeBytes { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public void UpdateDetails(
        string title,
        string? notes,
        string? referenceUrl,
        DateTime utcNow,
        string? composerName = null,
        string? instrumentName = null)
    {
        if (Status == RepertoireStatus.Archived)
            throw new InvalidOperationException("Archived repertoire items cannot be edited.");

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Repertoire title is required.", nameof(title));

        Title = title.Trim();
        Notes = NormalizeOptionalText(notes);
        ReferenceUrl = NormalizeOptionalText(referenceUrl);
        ComposerName = NormalizeOptionalText(composerName);
        InstrumentName = NormalizeOptionalText(instrumentName);
        UpdatedAtUtc = utcNow;
    }

    public void AttachAudio(
        string storedFileName,
        string originalFileName,
        string contentType,
        long sizeBytes,
        DateTime utcNow)
    {
        if (Status == RepertoireStatus.Archived)
            throw new InvalidOperationException("Archived repertoire items cannot be edited.");

        if (string.IsNullOrWhiteSpace(storedFileName))
            throw new ArgumentException("Stored audio file name is required.", nameof(storedFileName));

        if (string.IsNullOrWhiteSpace(originalFileName))
            throw new ArgumentException("Original audio file name is required.", nameof(originalFileName));

        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("Audio content type is required.", nameof(contentType));

        if (sizeBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(sizeBytes), "Audio size must be positive.");

        AudioStoredFileName = storedFileName.Trim();
        AudioOriginalFileName = originalFileName.Trim();
        AudioContentType = contentType.Trim();
        AudioSizeBytes = sizeBytes;
        UpdatedAtUtc = utcNow;
    }

    public void UpdateProgress(int progressPercent, DateTime utcNow)
    {
        if (Status == RepertoireStatus.Archived)
            throw new InvalidOperationException("Archived repertoire items cannot update progress.");

        if (progressPercent < 0 || progressPercent > 100)
            throw new ArgumentOutOfRangeException(nameof(progressPercent), "Progress must be between 0 and 100.");

        ProgressPercent = progressPercent;

        Status = progressPercent switch
        {
            0 => RepertoireStatus.NotStarted,
            >= 100 => RepertoireStatus.Learned,
            _ => RepertoireStatus.Practicing
        };

        UpdatedAtUtc = utcNow;
    }

    public void Archive(DateTime utcNow)
    {
        if (Status == RepertoireStatus.Archived)
            return;

        Status = RepertoireStatus.Archived;
        UpdatedAtUtc = utcNow;
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
