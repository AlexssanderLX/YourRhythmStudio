namespace YourRhythmStudio.Application.Users;

public sealed record CreateStudentRequest(
    Guid SchoolId,
    string DisplayName,
    string Email,
    Guid? AccountId = null,
    string Instrument = "",
    string Level = "",
    string Notes = "");
