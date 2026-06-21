using Foundation.Access.Tenancy;

namespace Foundation.Access.Abstractions;

public interface ITenantStore
{
    Task SaveAsync(Tenant tenant, CancellationToken cancellationToken = default);

    Task<Tenant?> FindByIdAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<Tenant?> FindByKeyAsync(string tenantKey, CancellationToken cancellationToken = default);

    Task UpdateAsync(Tenant tenant, CancellationToken cancellationToken = default);
}
