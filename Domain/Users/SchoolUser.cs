namespace YourRhythmStudio.Domain.Users;

public sealed class SchoolUser
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid SchoolId { get; init; }

    public School? School { get; init; }

    public Guid? AccountId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = YourRhythmRoles.Student;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
}
