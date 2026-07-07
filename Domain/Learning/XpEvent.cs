using YourRhythmStudio.Domain.Learning.Enums;

namespace YourRhythmStudio.Domain.Learning;

public sealed class XpEvent
{
    private XpEvent()
    {
    }

    public XpEvent(
        Guid schoolId,
        Guid studentProfileId,
        XpEventType type,
        int points,
        string description,
        DateTime utcNow,
        Guid? teacherProfileId = null,
        Guid? sourceId = null)
    {
        if (schoolId == Guid.Empty)
            throw new ArgumentException("SchoolId is required.", nameof(schoolId));

        if (studentProfileId == Guid.Empty)
            throw new ArgumentException("StudentProfileId is required.", nameof(studentProfileId));

        if (points == 0)
            throw new ArgumentOutOfRangeException(nameof(points), "XP points cannot be zero.");

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("XP event description is required.", nameof(description));

        Id = Guid.NewGuid();
        SchoolId = schoolId;
        StudentProfileId = studentProfileId;
        TeacherProfileId = teacherProfileId;
        SourceId = sourceId;
        Type = type;
        Points = points;
        Description = description.Trim();
        CreatedAtUtc = utcNow;
    }

    public Guid Id { get; private set; }

    public Guid SchoolId { get; private set; }

    public Guid StudentProfileId { get; private set; }

    public Guid? TeacherProfileId { get; private set; }

    public Guid? SourceId { get; private set; }

    public XpEventType Type { get; private set; }

    public int Points { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public DateTime CreatedAtUtc { get; private set; }
}