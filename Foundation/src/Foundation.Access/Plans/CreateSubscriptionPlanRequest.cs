namespace Foundation.Access.Plans;

public sealed record CreateSubscriptionPlanRequest(
    string Code,
    string DisplayName,
    string Description,
    decimal MonthlyPrice,
    decimal? YearlyPrice,
    decimal SetupFee,
    IReadOnlyCollection<string>? IncludedFeatures = null);
