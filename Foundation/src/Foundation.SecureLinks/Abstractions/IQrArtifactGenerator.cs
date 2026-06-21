using Foundation.SecureLinks.Models;

namespace Foundation.SecureLinks.Abstractions;

public interface IQrArtifactGenerator
{
    Task<QrArtifact> GenerateSvgAsync(string payload, string fileName, CancellationToken cancellationToken = default);
}
