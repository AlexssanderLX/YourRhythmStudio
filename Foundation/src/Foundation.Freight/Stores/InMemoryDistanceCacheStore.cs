using System.Collections.Concurrent;
using Foundation.Freight.Abstractions;
using Foundation.Freight.Models;

namespace Foundation.Freight.Stores;

public sealed class InMemoryDistanceCacheStore : IDistanceCacheStore
{
    private readonly ConcurrentDictionary<string, CachedDistanceEntry> _storage = new(StringComparer.OrdinalIgnoreCase);

    public Task<CachedDistanceEntry?> FindAsync(string cacheKey, CancellationToken cancellationToken = default)
    {
        _storage.TryGetValue(cacheKey, out var entry);
        return Task.FromResult(entry);
    }

    public Task SaveAsync(CachedDistanceEntry entry, CancellationToken cancellationToken = default)
    {
        _storage[entry.CacheKey] = entry;
        return Task.CompletedTask;
    }
}
