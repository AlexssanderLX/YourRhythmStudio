using System.Collections.Concurrent;
using Foundation.SecureLinks.Abstractions;
using Foundation.SecureLinks.Models;

namespace Foundation.SecureLinks.Stores;

public sealed class InMemorySecureLinkStore : ISecureLinkStore
{
    private readonly ConcurrentDictionary<string, SecureLinkRecord> _storage = new(StringComparer.OrdinalIgnoreCase);

    public Task SaveAsync(SecureLinkRecord link, CancellationToken cancellationToken = default)
    {
        _storage[link.PublicCode] = link;
        return Task.CompletedTask;
    }

    public Task<SecureLinkRecord?> FindByPublicCodeAsync(string publicCode, CancellationToken cancellationToken = default)
    {
        _storage.TryGetValue(publicCode, out var value);
        return Task.FromResult(value);
    }

    public Task UpdateAsync(SecureLinkRecord link, CancellationToken cancellationToken = default)
    {
        _storage[link.PublicCode] = link;
        return Task.CompletedTask;
    }
}
