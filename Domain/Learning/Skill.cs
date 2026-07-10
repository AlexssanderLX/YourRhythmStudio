using YourRhythmStudio.Domain.Learning.Enums;

namespace YourRhythmStudio.Domain.Learning;

public sealed class Skill
{
    private Skill() { }

    public Skill(
        Guid schoolId,
        Guid teacherProfileId,
        string name,
        string? description,
        int requiredLevel,
        SkillType skillType,
        string? iconName,
        string? achievementText,
        string? conquestCriteria,
        int sortOrder,
        DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        if (requiredLevel is < 1 or > 5) throw new ArgumentOutOfRangeException(nameof(requiredLevel));

        Id = Guid.NewGuid();
        SchoolId = schoolId;
        TeacherProfileId = teacherProfileId;
        Name = name.Trim();
        Description = description?.Trim();
        RequiredLevel = requiredLevel;
        SkillType = skillType;
        IconName = iconName?.Trim();
        AchievementText = achievementText?.Trim();
        ConquestCriteria = conquestCriteria?.Trim();
        SortOrder = sortOrder;
        IsActive = true;
        CreatedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public Guid Id { get; private set; }
    public Guid SchoolId { get; private set; }
    public Guid TeacherProfileId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int RequiredLevel { get; private set; }
    public SkillType SkillType { get; private set; }
    public string? IconName { get; private set; }
    public string? AchievementText { get; private set; }
    public string? ConquestCriteria { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public void Update(
        string name,
        string? description,
        SkillType skillType,
        string? iconName,
        string? achievementText,
        string? conquestCriteria,
        DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        Name = name.Trim();
        Description = description?.Trim();
        SkillType = skillType;
        IconName = iconName?.Trim();
        AchievementText = achievementText?.Trim();
        ConquestCriteria = conquestCriteria?.Trim();
        UpdatedAtUtc = utcNow;
    }

    public void Deactivate(DateTime utcNow)
    {
        IsActive = false;
        UpdatedAtUtc = utcNow;
    }

    public void Reactivate(DateTime utcNow)
    {
        IsActive = true;
        UpdatedAtUtc = utcNow;
    }
}
