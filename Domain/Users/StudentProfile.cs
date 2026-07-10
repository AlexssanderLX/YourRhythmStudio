namespace YourRhythmStudio.Domain.Users;

public sealed class StudentProfile
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid SchoolId { get; init; }

    public School? School { get; init; }

    public Guid SchoolUserId { get; init; }

    public SchoolUser? SchoolUser { get; init; }

    public string Instrument { get; set; } = string.Empty;

    public string Level { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;

    /// <summary>Cumulative total XP earned across all levels. Never resets. Used only for stats display.</summary>
    public int CurrentXp { get; set; }

    /// <summary>XP earned within the current level only. Resets to 0 on each promotion.</summary>
    public int CurrentLevelXp { get; set; }

    public int CurrentLevel { get; set; } = 1;
}
