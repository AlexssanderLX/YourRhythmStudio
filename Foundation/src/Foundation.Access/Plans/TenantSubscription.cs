namespace Foundation.Access.Plans;

public sealed class TenantSubscription
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid TenantId { get; init; }

    public Guid PlanId { get; set; }

    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;

    public string BillingEmail { get; set; } = string.Empty;

    public DateTime StartsAtUtc { get; set; }

    public DateTime? TrialEndsAtUtc { get; set; }

    public DateTime? EndsAtUtc { get; set; }

    public bool IsActive(DateTime utcNow)
    {
        if (Status == SubscriptionStatus.Canceled || Status == SubscriptionStatus.Suspended)
        {
            return false;
        }

        if (EndsAtUtc is not null && utcNow >= EndsAtUtc.Value)
        {
            return false;
        }

        return Status == SubscriptionStatus.Active || Status == SubscriptionStatus.Trial;
    }
}
