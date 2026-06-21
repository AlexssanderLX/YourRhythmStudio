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

    public int CurrentXp { get; set; }

    public int CurrentLevel { get; set; } = 1;
}
