using Foundation.Core.Models;
using Foundation.Freight.Models;

namespace Foundation.Freight.Abstractions;

public interface IDistanceProvider
{
    Task<OperationResult<DistanceQuote>> GetDistanceAsync(DistanceRequest request, CancellationToken cancellationToken = default);
}
