using System.Collections.Concurrent;
using Foundation.Access.Abstractions;
using Foundation.Access.Tenancy;

namespace Foundation.Access.Stores;

public sealed class InMemoryTenantStore : ITenantStore
{
    private readonly ConcurrentDictionary<Guid, Tenant> _storage = new();

    public Task SaveAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        _storage[tenant.Id] = tenant;
        return Task.CompletedTask;
    }

    public Task<Tenant?> FindByIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        _storage.TryGetValue(tenantId, out var tenant);
        return Task.FromResult(tenant);
    }

    public Task<Tenant?> FindByKeyAsync(string tenantKey, CancellationToken cancellationToken = default)
    {
        var normalizedKey = NormalizeKey(tenantKey);
        var tenant = _storage.Values.FirstOrDefault(item => NormalizeKey(item.Key) == normalizedKey);
        return Task.FromResult(tenant);
    }

    public Task UpdateAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        _storage[tenant.Id] = tenant;
        return Task.CompletedTask;
    }

    private static string NormalizeKey(string tenantKey) => tenantKey.Trim().ToUpperInvariant();
}
