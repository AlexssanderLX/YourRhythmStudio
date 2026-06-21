namespace YourRhythmStudio.Application.Users;

public sealed record SchoolSummary(
    Guid Id,
    string Name,
    string Slug,
    string PrimaryEmail,
    bool IsActive,
    int TeacherCount,
    int StudentCount);
