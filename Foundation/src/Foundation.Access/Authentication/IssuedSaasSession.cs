using Foundation.Access.Accounts;
using Foundation.Access.Tenancy;

namespace Foundation.Access.Authentication;

public sealed record IssuedSaasSession(
    Guid SessionId,
    string Token,
    Guid AccountId,
    string DisplayName,
    string Email,
    PlatformAccessRole PlatformRole,
    Guid? TenantId,
    string? TenantDisplayName,
    TenantAccessRole? TenantRole,
    string? PlanCode,
    IReadOnlyCollection<string> EnabledFeatures,
    DateTime ExpiresAtUtc);
