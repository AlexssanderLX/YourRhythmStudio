namespace YourRhythmStudio.Application.Users;

public sealed record CreateTeacherRequest(
    Guid SchoolId,
    string DisplayName,
    string Email,
    Guid? AccountId = null,
    string InstrumentFocus = "",
    string Bio = "");
