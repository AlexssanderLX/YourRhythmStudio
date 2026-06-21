using Foundation.Freight.Models;

namespace Foundation.Freight.Abstractions;

public interface IDistanceCacheStore
{
    Task<CachedDistanceEntry?> FindAsync(string cacheKey, CancellationToken cancellationToken = default);

    Task SaveAsync(CachedDistanceEntry entry, CancellationToken cancellationToken = default);
}
