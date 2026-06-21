using Foundation.Core.Abstractions;
using Foundation.Core.Models;
using Foundation.Core.Utilities;
using Foundation.Freight.Abstractions;
using Foundation.Freight.Models;
using Foundation.Freight.Options;

namespace Foundation.Freight.Services;

public sealed class FreightQuoteService
{
    private readonly IDistanceProvider _distanceProvider;
    private readonly IDistanceCacheStore _cacheStore;
    private readonly FreightModuleOptions _options;
    private readonly IClock _clock;

    public FreightQuoteService(
        IDistanceProvider distanceProvider,
        IDistanceCacheStore cacheStore,
        FreightModuleOptions options,
        IClock clock)
    {
        _distanceProvider = distanceProvider;
        _cacheStore = cacheStore;
        _options = options;
        _clock = clock;
    }

    public async Task<OperationResult<FreightQuote>> QuoteAsync(
        DistanceRequest request,
        FreightPolicy policy,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Guard.AgainstNegative(policy.BaseFee, nameof(policy.BaseFee));
            Guard.AgainstNegative(policy.RatePerKm, nameof(policy.RatePerKm));
            Guard.AgainstNegative(policy.MinimumCharge, nameof(policy.MinimumCharge));
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return OperationResult<FreightQuote>.Failure(OperationError.Validation(exception.Message));
        }

        var cacheKey = BuildCacheKey(request);
        var cached = await _cacheStore.FindAsync(cacheKey, cancellationToken);

        DistanceQuote distance;
        var cacheHit = false;

        if (cached is not null && cached.ExpiresAtUtc > _clock.UtcNow)
        {
            distance = new DistanceQuote(cached.DistanceKm, cached.ProviderName, cached.IsApproximate);
            cacheHit = true;
        }
        else
        {
            var distanceResult = await _distanceProvider.GetDistanceAsync(request, cancellationToken);
            if (distanceResult.IsFailure || distanceResult.Value is null)
            {
                return OperationResult<FreightQuote>.Failure(distanceResult.Error ?? OperationError.External("Falha ao calcular distancia."));
            }

            distance = distanceResult.Value;

            await _cacheStore.SaveAsync(
                new CachedDistanceEntry(
                    cacheKey,
                    distance.DistanceKm,
                    distance.ProviderName,
                    distance.IsApproximate,
                    _clock.UtcNow.Add(_options.CacheTimeToLive)),
                cancellationToken);
        }

        var amount = policy.BaseFee + (distance.DistanceKm * policy.RatePerKm);
        if (amount < policy.MinimumCharge)
        {
            amount = policy.MinimumCharge;
        }

        amount = decimal.Round(amount, policy.RoundDigits, MidpointRounding.AwayFromZero);

        return OperationResult<FreightQuote>.Success(
            new FreightQuote(distance.DistanceKm, amount, policy.BaseFee, policy.RatePerKm, distance.ProviderName, cacheHit, distance.IsApproximate));
    }

    private static string BuildCacheKey(DistanceRequest request)
    {
        return $"{NormalizePostalCode(request.Origin.PostalCode)}->{NormalizePostalCode(request.Destination.PostalCode)}";
    }

    private static string NormalizePostalCode(string postalCode)
    {
        return new string(postalCode.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
    }
}
