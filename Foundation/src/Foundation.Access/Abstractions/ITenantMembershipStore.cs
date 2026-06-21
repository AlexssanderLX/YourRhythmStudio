using Foundation.Access.Tenancy;

namespace Foundation.Access.Abstractions;

public interface ITenantMembershipStore
{
    Task SaveAsync(TenantMembership membership, CancellationToken cancellationToken = default);

    Task<TenantMembership?> FindByAccountAndTenantAsync(Guid accountId, Guid tenantId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<TenantMembership>> ListByAccountAsync(Guid accountId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<TenantMembership>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task UpdateAsync(TenantMembership membership, CancellationToken cancellationToken = default);
}
