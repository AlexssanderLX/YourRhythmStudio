using Foundation.Access.Registrations;

namespace Foundation.Access.Abstractions;

public interface IRegistrationRequestStore
{
    Task SaveAsync(TenantRegistrationRequest request, CancellationToken cancellationToken = default);

    Task<TenantRegistrationRequest?> FindByIdAsync(Guid requestId, CancellationToken cancellationToken = default);

    Task<TenantRegistrationRequest?> FindPendingByEmailAsync(string ownerEmail, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<TenantRegistrationRequest>> ListPendingAsync(CancellationToken cancellationToken = default);

    Task UpdateAsync(TenantRegistrationRequest request, CancellationToken cancellationToken = default);
}
