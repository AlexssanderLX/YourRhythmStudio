namespace YourRhythmStudio.Domain.Learning;

public sealed class LevelConfig
{
    private LevelConfig() { }

    public LevelConfig(
        Guid schoolId,
        Guid teacherProfileId,
        int level,
        DateTime utcNow)
    {
        if (level is < 1 or > 5) throw new ArgumentOutOfRangeException(nameof(level));

        Id = Guid.NewGuid();
        SchoolId = schoolId;
        TeacherProfileId = teacherProfileId;
        Level = level;
        CreatedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public Guid Id { get; private set; }
    public Guid SchoolId { get; private set; }
    public Guid TeacherProfileId { get; private set; }
    public int Level { get; private set; }

    public string? Subtitle { get; private set; }
    public string? Description { get; private set; }
    public string? TeacherExpectations { get; private set; }
    public string? Objectives { get; private set; }
    public string? ConquestMessage { get; private set; }
    public string? OrientationMessage { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public void Update(
        string? subtitle,
        string? description,
        string? teacherExpectations,
        string? objectives,
        string? conquestMessage,
        string? orientationMessage,
        DateTime utcNow)
    {
        Subtitle = Trim(subtitle);
        Description = Trim(description);
        TeacherExpectations = Trim(teacherExpectations);
        Objectives = Trim(objectives);
        ConquestMessage = Trim(conquestMessage);
        OrientationMessage = Trim(orientationMessage);
        UpdatedAtUtc = utcNow;
    }

    private static string? Trim(string? v) =>
        string.IsNullOrWhiteSpace(v) ? null : v.Trim();
}
