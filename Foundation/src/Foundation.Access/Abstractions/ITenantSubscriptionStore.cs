using Foundation.Access.Plans;

namespace Foundation.Access.Abstractions;

public interface ITenantSubscriptionStore
{
    Task SaveAsync(TenantSubscription subscription, CancellationToken cancellationToken = default);

    Task<TenantSubscription?> FindCurrentByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task UpdateAsync(TenantSubscription subscription, CancellationToken cancellationToken = default);
}
