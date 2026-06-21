using Foundation.Access.Plans;

namespace Foundation.Access.Abstractions;

public interface ISubscriptionPlanStore
{
    Task SaveAsync(SubscriptionPlan plan, CancellationToken cancellationToken = default);

    Task<SubscriptionPlan?> FindByIdAsync(Guid planId, CancellationToken cancellationToken = default);

    Task<SubscriptionPlan?> FindByCodeAsync(string planCode, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<SubscriptionPlan>> ListActiveAsync(CancellationToken cancellationToken = default);

    Task UpdateAsync(SubscriptionPlan plan, CancellationToken cancellationToken = default);
}
