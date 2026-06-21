namespace YourRhythmStudio.Application.Users;

public sealed record TeacherSummary(
    Guid Id,
    Guid SchoolUserId,
    string DisplayName,
    string Email,
    string InstrumentFocus,
    bool IsActive);
