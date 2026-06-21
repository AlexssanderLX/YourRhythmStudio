namespace Foundation.Access.Registrations;

public sealed record RegistrationReviewRequestedMessage(
    IReadOnlyCollection<string> ReviewRecipients,
    Guid RegistrationRequestId,
    string TenantDisplayName,
    string TenantKey,
    string OwnerDisplayName,
    string OwnerEmail,
    string? RequestedPlanCode,
    DateTime SubmittedAtUtc);
