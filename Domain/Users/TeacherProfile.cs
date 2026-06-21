namespace YourRhythmStudio.Domain.Users;

public sealed class TeacherProfile
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid SchoolId { get; init; }

    public School? School { get; init; }

    public Guid SchoolUserId { get; init; }

    public SchoolUser? SchoolUser { get; init; }

    public string InstrumentFocus { get; set; } = string.Empty;

    public string Bio { get; set; } = string.Empty;

    public bool CanManageStudents { get; set; } = true;
}
