namespace Foundation.Access.Models;

public sealed record IssuedSession(
    Guid SessionId,
    string Token,
    string SubjectId,
    string SubjectDisplayName,
    string Purpose,
    DateTime ExpiresAtUtc);
