using Foundation.SecureLinks.Models;

namespace Foundation.SecureLinks.Abstractions;

public interface ISecureLinkStore
{
    Task SaveAsync(SecureLinkRecord link, CancellationToken cancellationToken = default);

    Task<SecureLinkRecord?> FindByPublicCodeAsync(string publicCode, CancellationToken cancellationToken = default);

    Task UpdateAsync(SecureLinkRecord link, CancellationToken cancellationToken = default);
}
