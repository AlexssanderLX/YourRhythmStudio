namespace Foundation.Freight.Models;

public sealed record FreightQuote(
    decimal DistanceKm,
    decimal FreightAmount,
    decimal BaseFee,
    decimal RatePerKm,
    string ProviderName,
    bool CacheHit,
    bool IsApproximate);
