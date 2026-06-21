namespace Foundation.Access.Tenancy;

public sealed class TenantMembership
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid TenantId { get; init; }

    public Guid AccountId { get; init; }

    public TenantAccessRole Role { get; set; } = TenantAccessRole.Member;

    public bool IsPrimary { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime JoinedAtUtc { get; init; }
}
