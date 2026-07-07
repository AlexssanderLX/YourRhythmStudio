using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using YourRhythmStudio.Domain;
using YourRhythmStudio.Infrastructure.Data;

namespace YourRhythmStudio.Application.Users;

public sealed class UserProfileResolver : IUserProfileResolver
{
    public const string SchoolIdClaim = "SchoolId";
    public const string SchoolUserIdClaim = "SchoolUserId";
    public const string TeacherProfileIdClaim = "TeacherProfileId";
    public const string StudentProfileIdClaim = "StudentProfileId";
    public const string RoleClaim = "YourRhythmRole";

    private readonly YourRhythmDbContext _dbContext;

    public UserProfileResolver(YourRhythmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AuthenticatedUserProfile?> ResolveForSignInAsync(
        Guid accountId,
        string email,
        string displayName,
        string? fallbackRole,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(email);

        var schoolUser = await _dbContext.SchoolUsers
            .AsNoTracking()
            .Where(user => user.IsActive)
            .FirstOrDefaultAsync(
                user => user.AccountId == accountId || user.Email == normalizedEmail,
                cancellationToken);

        if (schoolUser is null)
        {
            return fallbackRole is null
                ? null
                : new AuthenticatedUserProfile(accountId, normalizedEmail, displayName, fallbackRole, null, null, null, null);
        }

        var teacherProfileId = schoolUser.Role == YourRhythmRoles.Teacher
            ? await _dbContext.TeacherProfiles
                .AsNoTracking()
                .Where(profile => profile.SchoolId == schoolUser.SchoolId && profile.SchoolUserId == schoolUser.Id)
                .Select(profile => (Guid?)profile.Id)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        var studentProfileId = schoolUser.Role == YourRhythmRoles.Student
            ? await _dbContext.StudentProfiles
                .AsNoTracking()
                .Where(profile => profile.SchoolId == schoolUser.SchoolId && profile.SchoolUserId == schoolUser.Id)
                .Select(profile => (Guid?)profile.Id)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        return new AuthenticatedUserProfile(
            accountId,
            schoolUser.Email,
            schoolUser.DisplayName,
            schoolUser.Role,
            schoolUser.SchoolId,
            schoolUser.Id,
            teacherProfileId,
            studentProfileId);
    }

    public Task<AuthenticatedUserProfile> ResolveCurrentAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var accountId = GetRequiredGuidClaim(user, ClaimTypes.NameIdentifier);
        var email = user.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        var displayName = user.Identity?.Name ?? user.FindFirstValue(ClaimTypes.Name) ?? email;
        var role = user.FindFirstValue(RoleClaim)
            ?? throw new UnauthorizedAccessException("YourRhythm role claim is missing.");

        var profile = new AuthenticatedUserProfile(
            accountId,
            email,
            displayName,
            role,
            GetOptionalGuidClaim(user, SchoolIdClaim),
            GetOptionalGuidClaim(user, SchoolUserIdClaim),
            GetOptionalGuidClaim(user, TeacherProfileIdClaim),
            GetOptionalGuidClaim(user, StudentProfileIdClaim));

        return Task.FromResult(profile);
    }

    private static Guid GetRequiredGuidClaim(ClaimsPrincipal user, string claimType)
    {
        var value = user.FindFirstValue(claimType);
        if (!Guid.TryParse(value, out var parsed))
        {
            throw new UnauthorizedAccessException($"{claimType} claim is missing or invalid.");
        }

        return parsed;
    }

    private static Guid? GetOptionalGuidClaim(ClaimsPrincipal user, string claimType)
    {
        var value = user.FindFirstValue(claimType);
        return Guid.TryParse(value, out var parsed) ? parsed : null;
    }

    private static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();
}

