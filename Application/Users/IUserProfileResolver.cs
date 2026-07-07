using System.Security.Claims;

namespace YourRhythmStudio.Application.Users;

public interface IUserProfileResolver
{
    Task<AuthenticatedUserProfile?> ResolveForSignInAsync(
        Guid accountId,
        string email,
        string displayName,
        string? fallbackRole,
        CancellationToken cancellationToken = default);

    Task<AuthenticatedUserProfile> ResolveCurrentAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);
}

