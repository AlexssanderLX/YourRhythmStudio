using Foundation.Core.Models;
using Foundation.Freight.Models;

namespace Foundation.Freight.Abstractions;

public interface IAddressCoordinateResolver
{
    Task<OperationResult<GeoPoint>> ResolveAsync(PostalAddress address, CancellationToken cancellationToken = default);
}
