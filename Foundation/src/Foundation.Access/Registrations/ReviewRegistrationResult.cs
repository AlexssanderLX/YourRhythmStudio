namespace Foundation.Access.Registrations;

public sealed record ReviewRegistrationResult(
    Guid RegistrationRequestId,
    RegistrationRequestStatus Status,
    Guid? AccountId,
    Guid? TenantId,
    Guid? SubscriptionId);
