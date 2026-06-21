using Foundation.Access.Accounts;
using Foundation.Access.Tenancy;

namespace Foundation.Access.Models;

public sealed class SessionTicket
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string SubjectId { get; init; } = string.Empty;

    public string SubjectDisplayName { get; init; } = string.Empty;

    public Guid? AccountId { get; init; }

    public string? Email { get; init; }

    public string Purpose { get; init; } = string.Empty;

    public Guid? TenantId { get; init; }

    public string? TenantDisplayName { get; init; }

    public PlatformAccessRole PlatformRole { get; init; } = PlatformAccessRole.None;

    public TenantAccessRole? TenantRole { get; init; }

    public string? PlanCode { get; init; }

    public string TokenHash { get; init; } = string.Empty;

    public DateTime CreatedAtUtc { get; init; }

    public DateTime ExpiresAtUtc { get; init; }

    public DateTime? RevokedAtUtc { get; set; }

    public bool IsValid(DateTime utcNow) => RevokedAtUtc is null && utcNow < ExpiresAtUtc;
}
