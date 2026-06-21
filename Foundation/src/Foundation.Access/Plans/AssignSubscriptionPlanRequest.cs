namespace Foundation.Access.Plans;

public sealed record AssignSubscriptionPlanRequest(
    Guid TenantId,
    Guid PlanId,
    Guid UpdatedByAccountId,
    string? BillingEmail = null,
    bool StartTrial = false,
    int? OverrideTrialDays = null);
