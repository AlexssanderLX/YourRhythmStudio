namespace Foundation.Freight.Models;

public sealed record CachedDistanceEntry(
    string CacheKey,
    decimal DistanceKm,
    string ProviderName,
    bool IsApproximate,
    DateTime ExpiresAtUtc);
