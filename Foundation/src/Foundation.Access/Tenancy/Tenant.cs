namespace Foundation.Access.Tenancy;

public sealed class Tenant
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Key { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string PrimaryEmail { get; set; } = string.Empty;

    public TenantStatus Status { get; set; } = TenantStatus.PendingApproval;

    public Guid? OwnerAccountId { get; set; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? ApprovedAtUtc { get; set; }

    public bool IsActive => Status == TenantStatus.Active;
}
