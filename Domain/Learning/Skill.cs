namespace YourRhythmStudio.Domain.Learning;

public sealed class Skill
{
    private Skill() { }

    public Skill(Guid schoolId, Guid teacherProfileId, string name, string? description, int requiredLevel, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        if (requiredLevel is < 1 or > 5) throw new ArgumentOutOfRangeException(nameof(requiredLevel));

        Id = Guid.NewGuid();
        SchoolId = schoolId;
        TeacherProfileId = teacherProfileId;
        Name = name.Trim();
        Description = description?.Trim();
        RequiredLevel = requiredLevel;
        CreatedAtUtc = utcNow;
    }

    public Guid Id { get; private set; }
    public Guid SchoolId { get; private set; }
    public Guid TeacherProfileId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int RequiredLevel { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
}
