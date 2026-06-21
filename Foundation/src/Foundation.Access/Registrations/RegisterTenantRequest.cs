namespace Foundation.Access.Registrations;

public sealed record RegisterTenantRequest(
    string TenantDisplayName,
    string? TenantKey,
    string OwnerDisplayName,
    string OwnerEmail,
    string Password,
    string? RequestedPlanCode = null);
