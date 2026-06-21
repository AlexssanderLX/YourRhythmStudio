namespace Foundation.Access.Plans;

public sealed class SubscriptionPlan
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Code { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal MonthlyPrice { get; set; }

    public decimal? YearlyPrice { get; set; }

    public decimal SetupFee { get; set; }

    public List<string> IncludedFeatures { get; set; } = [];

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; init; }
}
