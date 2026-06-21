namespace Foundation.Access.Models;

public sealed record AccessCodeRequest(
    string SubjectId,
    string SubjectDisplayName,
    string Recipient,
    string Purpose);
