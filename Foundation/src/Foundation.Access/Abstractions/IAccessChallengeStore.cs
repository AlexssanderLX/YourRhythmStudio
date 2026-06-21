using Foundation.Access.Models;

namespace Foundation.Access.Abstractions;

public interface IAccessChallengeStore
{
    Task SaveAsync(AccessChallenge challenge, CancellationToken cancellationToken = default);

    Task<AccessChallenge?> FindByIdAsync(Guid challengeId, CancellationToken cancellationToken = default);

    Task UpdateAsync(AccessChallenge challenge, CancellationToken cancellationToken = default);
}
