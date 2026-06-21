using System.Collections.Concurrent;
using Foundation.Core.Models;
using Foundation.Freight.Abstractions;
using Foundation.Freight.Models;

namespace Foundation.Freight.Providers;

public sealed class InMemoryAddressCoordinateResolver : IAddressCoordinateResolver
{
    private readonly ConcurrentDictionary<string, GeoPoint> _points = new(StringComparer.OrdinalIgnoreCase);

    public void Register(string postalCode, GeoPoint point)
    {
        _points[Normalize(postalCode)] = point;
    }

    public Task<OperationResult<GeoPoint>> ResolveAsync(PostalAddress address, CancellationToken cancellationToken = default)
    {
        var key = Normalize(address.PostalCode);

        if (_points.TryGetValue(key, out var point))
        {
            return Task.FromResult(OperationResult<GeoPoint>.Success(point));
        }

        return Task.FromResult(OperationResult<GeoPoint>.Failure(OperationError.NotFound($"CEP ou codigo postal nao encontrado: {address.PostalCode}")));
    }

    private static string Normalize(string postalCode)
    {
        return new string(postalCode.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
    }
}
