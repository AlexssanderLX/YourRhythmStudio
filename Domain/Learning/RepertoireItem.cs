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
        string? composerOrArtist,
        string? instrument,
        string? level,
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
        ComposerOrArtist = NormalizeOptionalText(composerOrArtist);
        Instrument = NormalizeOptionalText(instrument);
        Level = NormalizeOptionalText(level);
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

    public string? ComposerOrArtist { get; private set; }

    public string? Instrument { get; private set; }

    public string? Level { get; private set; }

    public RepertoireStatus Status { get; private set; }

    public int ProgressPercent { get; private set; }

    public string? Notes { get; private set; }

    public string? ReferenceUrl { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public void UpdateDetails(
        string title,
        string? composerOrArtist,
        string? instrument,
        string? level,
        string? notes,
        string? referenceUrl,
        DateTime utcNow)
    {
        if (Status == RepertoireStatus.Archived)
            throw new InvalidOperationException("Archived repertoire items cannot be edited.");

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Repertoire title is required.", nameof(title));

        Title = title.Trim();
        ComposerOrArtist = NormalizeOptionalText(composerOrArtist);
        Instrument = NormalizeOptionalText(instrument);
        Level = NormalizeOptionalText(level);
        Notes = NormalizeOptionalText(notes);
        ReferenceUrl = NormalizeOptionalText(referenceUrl);
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