using System.Collections.Concurrent;
using Foundation.Access.Abstractions;
using Foundation.Access.Tenancy;

namespace Foundation.Access.Stores;

public sealed class InMemoryTenantMembershipStore : ITenantMembershipStore
{
    private readonly ConcurrentDictionary<Guid, TenantMembership> _storage = new();

    public Task SaveAsync(TenantMembership membership, CancellationToken cancellationToken = default)
    {
        _storage[membership.Id] = membership;
        return Task.CompletedTask;
    }

    public Task<TenantMembership?> FindByAccountAndTenantAsync(Guid accountId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var membership = _storage.Values.FirstOrDefault(item => item.AccountId == accountId && item.TenantId == tenantId);
        return Task.FromResult(membership);
    }

    public Task<IReadOnlyCollection<TenantMembership>> ListByAccountAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<TenantMembership> memberships = _storage.Values.Where(item => item.AccountId == accountId).ToArray();
        return Task.FromResult(memberships);
    }

    public Task<IReadOnlyCollection<TenantMembership>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<TenantMembership> memberships = _storage.Values.Where(item => item.TenantId == tenantId).ToArray();
        return Task.FromResult(memberships);
    }

    public Task UpdateAsync(TenantMembership membership, CancellationToken cancellationToken = default)
    {
        _storage[membership.Id] = membership;
        return Task.CompletedTask;
    }
}
