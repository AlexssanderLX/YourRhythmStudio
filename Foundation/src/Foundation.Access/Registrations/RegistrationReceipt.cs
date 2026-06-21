namespace Foundation.Access.Registrations;

public sealed record RegistrationReceipt(
    Guid RegistrationRequestId,
    string OwnerEmail,
    bool RequiresApproval,
    DateTime SubmittedAtUtc);
