namespace YourRhythmStudio.Domain.Users;

public sealed class School
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string PrimaryEmail { get; set; } = string.Empty;

    public Guid? OwnerAccountId { get; set; }

    public string PlanCode { get; set; } = "professor";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;

    public List<SchoolUser> Users { get; } = [];

    public List<TeacherProfile> Teachers { get; } = [];

    public List<StudentProfile> Students { get; } = [];
}
