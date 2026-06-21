namespace YourRhythmStudio.Application.Users;

public sealed record CreateSchoolRequest(
    string Name,
    string PrimaryEmail,
    Guid? OwnerAccountId = null);
