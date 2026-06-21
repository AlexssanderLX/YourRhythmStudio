using Foundation.Access.Models;

namespace Foundation.Access.Abstractions;

public interface ISessionTicketStore
{
    Task SaveAsync(SessionTicket session, CancellationToken cancellationToken = default);

    Task<SessionTicket?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task<SessionTicket?> FindByIdAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<SessionTicket>> ListByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default);

    Task UpdateAsync(SessionTicket session, CancellationToken cancellationToken = default);
}
