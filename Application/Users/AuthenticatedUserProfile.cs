namespace YourRhythmStudio.Application.Users;

public sealed record AuthenticatedUserProfile(
    Guid AccountId,
    string Email,
    string DisplayName,
    string Role,
    Guid? SchoolId,
    Guid? SchoolUserId,
    Guid? TeacherProfileId,
    Guid? StudentProfileId);

