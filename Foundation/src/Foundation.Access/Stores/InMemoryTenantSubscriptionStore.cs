using System.Collections.Concurrent;
using Foundation.Access.Abstractions;
using Foundation.Access.Plans;

namespace Foundation.Access.Stores;

public sealed class InMemoryTenantSubscriptionStore : ITenantSubscriptionStore
{
    private readonly ConcurrentDictionary<Guid, TenantSubscription> _storage = new();

    public Task SaveAsync(TenantSubscription subscription, CancellationToken cancellationToken = default)
    {
        _storage[subscription.Id] = subscription;
        return Task.CompletedTask;
    }

    public Task<TenantSubscription?> FindCurrentByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var subscription = _storage.Values
            .Where(item => item.TenantId == tenantId)
            .OrderByDescending(item => item.StartsAtUtc)
            .FirstOrDefault();

        return Task.FromResult(subscription);
    }

    public Task UpdateAsync(TenantSubscription subscription, CancellationToken cancellationToken = default)
    {
        _storage[subscription.Id] = subscription;
        return Task.CompletedTask;
    }
}
