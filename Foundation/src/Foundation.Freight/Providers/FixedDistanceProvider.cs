using Foundation.Core.Models;
using Foundation.Freight.Abstractions;
using Foundation.Freight.Models;

namespace Foundation.Freight.Providers;

public sealed class FixedDistanceProvider : IDistanceProvider
{
    private readonly decimal _distanceKm;
    private readonly string _providerName;

    public FixedDistanceProvider(decimal distanceKm, string providerName = "fixed")
    {
        _distanceKm = distanceKm;
        _providerName = providerName;
    }

    public Task<OperationResult<DistanceQuote>> GetDistanceAsync(DistanceRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(OperationResult<DistanceQuote>.Success(new DistanceQuote(_distanceKm, _providerName, true)));
    }
}
