using System.Collections.Concurrent;
using Foundation.Access.Abstractions;
using Foundation.Access.Models;

namespace Foundation.Access.Stores;

public sealed class InMemorySessionTicketStore : ISessionTicketStore
{
    private readonly ConcurrentDictionary<Guid, SessionTicket> _storage = new();

    public Task SaveAsync(SessionTicket session, CancellationToken cancellationToken = default)
    {
        _storage[session.Id] = session;
        return Task.CompletedTask;
    }

    public Task<SessionTicket?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        var session = _storage.Values.FirstOrDefault(item => item.TokenHash == tokenHash);
        return Task.FromResult(session);
    }

    public Task<SessionTicket?> FindByIdAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        _storage.TryGetValue(sessionId, out var session);
        return Task.FromResult(session);
    }

    public Task<IReadOnlyCollection<SessionTicket>> ListByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<SessionTicket> sessions = _storage.Values
            .Where(item => item.AccountId == accountId)
            .ToArray();

        return Task.FromResult(sessions);
    }

    public Task UpdateAsync(SessionTicket session, CancellationToken cancellationToken = default)
    {
        _storage[session.Id] = session;
        return Task.CompletedTask;
    }
}
