using Foundation.Core.Models;
using Foundation.Freight.Abstractions;
using Foundation.Freight.Models;

namespace Foundation.Freight.Providers;

public sealed class ApproximatePostalCodeDistanceProvider : IDistanceProvider
{
    private readonly IAddressCoordinateResolver _resolver;
    private readonly decimal _routeFactor;
    private readonly decimal _minimumDistanceKm;

    public ApproximatePostalCodeDistanceProvider(
        IAddressCoordinateResolver resolver,
        decimal routeFactor = 1.25m,
        decimal minimumDistanceKm = 0.5m)
    {
        _resolver = resolver;
        _routeFactor = routeFactor;
        _minimumDistanceKm = minimumDistanceKm;
    }

    public async Task<OperationResult<DistanceQuote>> GetDistanceAsync(DistanceRequest request, CancellationToken cancellationToken = default)
    {
        var originResult = await _resolver.ResolveAsync(request.Origin, cancellationToken);
        if (originResult.IsFailure || originResult.Value is null)
        {
            return OperationResult<DistanceQuote>.Failure(originResult.Error ?? OperationError.NotFound("Origem nao encontrada."));
        }

        var destinationResult = await _resolver.ResolveAsync(request.Destination, cancellationToken);
        if (destinationResult.IsFailure || destinationResult.Value is null)
        {
            return OperationResult<DistanceQuote>.Failure(destinationResult.Error ?? OperationError.NotFound("Destino nao encontrado."));
        }

        var beeline = CalculateHaversineKm(originResult.Value, destinationResult.Value);
        var adjusted = Math.Max(_minimumDistanceKm, decimal.Round(beeline * _routeFactor, 2, MidpointRounding.AwayFromZero));

        return OperationResult<DistanceQuote>.Success(new DistanceQuote(adjusted, "approximate-postal-code", true));
    }

    private static decimal CalculateHaversineKm(GeoPoint origin, GeoPoint destination)
    {
        const double radiusKm = 6371.0;
        var lat1 = DegreesToRadians(origin.Latitude);
        var lat2 = DegreesToRadians(destination.Latitude);
        var deltaLat = DegreesToRadians(destination.Latitude - origin.Latitude);
        var deltaLon = DegreesToRadians(destination.Longitude - origin.Longitude);

        var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                Math.Cos(lat1) * Math.Cos(lat2) *
                Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return decimal.Round((decimal)(radiusKm * c), 2, MidpointRounding.AwayFromZero);
    }

    private static double DegreesToRadians(double degrees) => degrees * (Math.PI / 180d);
}
