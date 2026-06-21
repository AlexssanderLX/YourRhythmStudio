using System.Collections.Concurrent;
using Foundation.Access.Abstractions;
using Foundation.Access.Models;

namespace Foundation.Access.Stores;

public sealed class InMemoryAccessChallengeStore : IAccessChallengeStore
{
    private readonly ConcurrentDictionary<Guid, AccessChallenge> _storage = new();

    public Task SaveAsync(AccessChallenge challenge, CancellationToken cancellationToken = default)
    {
        _storage[challenge.Id] = challenge;
        return Task.CompletedTask;
    }

    public Task<AccessChallenge?> FindByIdAsync(Guid challengeId, CancellationToken cancellationToken = default)
    {
        _storage.TryGetValue(challengeId, out var challenge);
        return Task.FromResult(challenge);
    }

    public Task UpdateAsync(AccessChallenge challenge, CancellationToken cancellationToken = default)
    {
        _storage[challenge.Id] = challenge;
        return Task.CompletedTask;
    }
}
