namespace Foundation.Access.Registrations;

public sealed record RegistrationDecisionMessage(
    string Recipient,
    bool Approved,
    string TenantDisplayName,
    string OwnerDisplayName,
    string MessageBody,
    DateTime ReviewedAtUtc);
