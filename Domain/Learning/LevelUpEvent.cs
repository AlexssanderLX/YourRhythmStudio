namespace YourRhythmStudio.Domain.Learning;

public sealed class LevelUpEvent
{
    private LevelUpEvent() { }

    public LevelUpEvent(
        Guid schoolId,
        Guid studentProfileId,
        int fromLevel,
        int toLevel,
        DateTime utcNow)
    {
        Id = Guid.NewGuid();
        SchoolId = schoolId;
        StudentProfileId = studentProfileId;
        FromLevel = fromLevel;
        ToLevel = toLevel;
        CreatedAtUtc = utcNow;
    }

    public Guid Id { get; private set; }
    public Guid SchoolId { get; private set; }
    public Guid StudentProfileId { get; private set; }
    public int FromLevel { get; private set; }
    public int ToLevel { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? SeenAtUtc { get; private set; }

    public void MarkSeen(DateTime utcNow)
    {
        if (SeenAtUtc is null)
            SeenAtUtc = utcNow;
    }
}
