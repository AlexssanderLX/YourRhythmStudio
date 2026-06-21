using System.Collections.Concurrent;
using Foundation.Access.Abstractions;
using Foundation.Access.Registrations;

namespace Foundation.Access.Stores;

public sealed class InMemoryRegistrationRequestStore : IRegistrationRequestStore
{
    private readonly ConcurrentDictionary<Guid, TenantRegistrationRequest> _storage = new();

    public Task SaveAsync(TenantRegistrationRequest request, CancellationToken cancellationToken = default)
    {
        _storage[request.Id] = request;
        return Task.CompletedTask;
    }

    public Task<TenantRegistrationRequest?> FindByIdAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        _storage.TryGetValue(requestId, out var request);
        return Task.FromResult(request);
    }

    public Task<TenantRegistrationRequest?> FindPendingByEmailAsync(string ownerEmail, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(ownerEmail);
        var request = _storage.Values.FirstOrDefault(item =>
            item.Status == RegistrationRequestStatus.Pending &&
            NormalizeEmail(item.OwnerEmail) == normalizedEmail);

        return Task.FromResult(request);
    }

    public Task<IReadOnlyCollection<TenantRegistrationRequest>> ListPendingAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<TenantRegistrationRequest> pending = _storage.Values
            .Where(item => item.Status == RegistrationRequestStatus.Pending)
            .OrderBy(item => item.CreatedAtUtc)
            .ToArray();

        return Task.FromResult(pending);
    }

    public Task UpdateAsync(TenantRegistrationRequest request, CancellationToken cancellationToken = default)
    {
        _storage[request.Id] = request;
        return Task.CompletedTask;
    }

    private static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();
}
