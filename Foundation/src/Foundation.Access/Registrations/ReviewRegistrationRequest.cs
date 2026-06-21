namespace Foundation.Access.Registrations;

public sealed record ReviewRegistrationRequest(
    Guid RegistrationRequestId,
    Guid ReviewedByAccountId,
    bool Approve,
    string? Notes = null);
