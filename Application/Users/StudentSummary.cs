namespace YourRhythmStudio.Application.Users;

public sealed record StudentSummary(
    Guid Id,
    Guid SchoolUserId,
    string DisplayName,
    string Email,
    string Instrument,
    string Level,
    int CurrentXp,
    int CurrentLevel,
    bool IsActive);
