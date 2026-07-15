using System.Security.Claims;
using Foundation.Access.Abstractions;
using Foundation.Access.Accounts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using YourRhythmStudio.Application.Users;
using YourRhythmStudio.Infrastructure.Data;

namespace YourRhythmStudio.Infrastructure.Auth;

public sealed class YourRhythmCookieEvents : CookieAuthenticationEvents
{
    private readonly AuthSessionOptions _options;
    private readonly IAccountStore _accountStore;
    private readonly ISessionTicketStore _sessionTicketStore;
    private readonly YourRhythmDbContext _db;
    private readonly ILogger<YourRhythmCookieEvents> _logger;

    public YourRhythmCookieEvents(
        IOptions<AuthSessionOptions> options,
        IAccountStore accountStore,
        ISessionTicketStore sessionTicketStore,
        YourRhythmDbContext db,
        ILogger<YourRhythmCookieEvents> logger)
    {
        _options = options.Value;
        _accountStore = accountStore;
        _sessionTicketStore = sessionTicketStore;
        _db = db;
        _logger = logger;
    }

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var now = DateTimeOffset.UtcNow;
        var principal = context.Principal;
        var identity = principal?.Identity as ClaimsIdentity;

        if (principal is null || identity is null || !identity.IsAuthenticated)
        {
            await RejectAsync(context, "missing principal");
            return;
        }

        if (!TryReadInstant(principal, YourRhythmAuthClaims.IssuedAtUtc, out var issuedAtUtc))
        {
            await RejectAsync(context, "missing issued-at claim");
            return;
        }

        if (now - issuedAtUtc > _options.AbsoluteTimeout)
        {
            await RejectAsync(context, "absolute session timeout");
            return;
        }

        if (TryReadInstant(principal, YourRhythmAuthClaims.LastValidatedAtUtc, out var lastValidatedAtUtc)
            && now - lastValidatedAtUtc < _options.ValidationInterval)
        {
            return;
        }

        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var subjectId))
        {
            await RejectAsync(context, "invalid subject id");
            return;
        }

        var account = await _accountStore.FindByIdAsync(subjectId, context.HttpContext.RequestAborted);
        if (account is not null)
        {
            if (!await ValidateAccountAsync(context, principal, account))
            {
                return;
            }
        }
        else if (!await ValidateSchoolUserAsync(context, principal))
        {
            return;
        }

        ReplaceClaim(identity, YourRhythmAuthClaims.LastValidatedAtUtc, ToUnixTime(now));
        context.ReplacePrincipal(new ClaimsPrincipal(identity));
        context.ShouldRenew = true;
    }

    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
    {
        if (string.Equals(context.Request.Headers.XRequestedWith, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }

        var redirectUri = QueryHelpers.AddQueryString(context.RedirectUri, "expired", "1");
        context.Response.Redirect(redirectUri);
        return Task.CompletedTask;
    }

    private async Task<bool> ValidateAccountAsync(CookieValidatePrincipalContext context, ClaimsPrincipal principal, Account account)
    {
        if (!account.IsActive)
        {
            await RejectAsync(context, "inactive account");
            return false;
        }

        var expectedStamp = account.SecurityStamp;
        var actualStamp = principal.FindFirstValue(YourRhythmAuthClaims.SecurityStamp);
        if (!string.IsNullOrWhiteSpace(expectedStamp)
            && !string.Equals(expectedStamp, actualStamp, StringComparison.Ordinal))
        {
            await RejectAsync(context, "security stamp mismatch");
            return false;
        }

        if (Guid.TryParse(principal.FindFirstValue("SessionId"), out var sessionId))
        {
            var session = await _sessionTicketStore.FindByIdAsync(sessionId, context.HttpContext.RequestAborted);
            if (session is not null && session.RevokedAtUtc is not null)
            {
                await RejectAsync(context, "revoked foundation session");
                return false;
            }
        }

        return true;
    }

    private async Task<bool> ValidateSchoolUserAsync(CookieValidatePrincipalContext context, ClaimsPrincipal principal)
    {
        if (!Guid.TryParse(principal.FindFirstValue(UserProfileResolver.SchoolUserIdClaim), out var schoolUserId))
        {
            await RejectAsync(context, "missing school user claim");
            return false;
        }

        var schoolUser = await _db.SchoolUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Id == schoolUserId, context.HttpContext.RequestAborted);

        if (schoolUser is null || !schoolUser.IsActive)
        {
            await RejectAsync(context, "inactive school user");
            return false;
        }

        var role = principal.FindFirstValue(UserProfileResolver.RoleClaim);
        if (!string.Equals(role, schoolUser.Role, StringComparison.Ordinal))
        {
            await RejectAsync(context, "school role mismatch");
            return false;
        }

        if (Guid.TryParse(principal.FindFirstValue(UserProfileResolver.StudentProfileIdClaim), out var studentProfileId))
        {
            var ownsStudentProfile = await _db.StudentProfiles
                .AsNoTracking()
                .AnyAsync(profile => profile.Id == studentProfileId
                                  && profile.SchoolId == schoolUser.SchoolId
                                  && profile.SchoolUserId == schoolUser.Id,
                    context.HttpContext.RequestAborted);

            if (!ownsStudentProfile)
            {
                await RejectAsync(context, "student profile mismatch");
                return false;
            }
        }

        if (Guid.TryParse(principal.FindFirstValue(UserProfileResolver.TeacherProfileIdClaim), out var teacherProfileId))
        {
            var ownsTeacherProfile = await _db.TeacherProfiles
                .AsNoTracking()
                .AnyAsync(profile => profile.Id == teacherProfileId
                                  && profile.SchoolId == schoolUser.SchoolId
                                  && profile.SchoolUserId == schoolUser.Id,
                    context.HttpContext.RequestAborted);

            if (!ownsTeacherProfile)
            {
                await RejectAsync(context, "teacher profile mismatch");
                return false;
            }
        }

        return true;
    }

    private async Task RejectAsync(CookieValidatePrincipalContext context, string reason)
    {
        _logger.LogInformation("Rejecting authentication cookie: {Reason}", reason);
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(context.Scheme.Name);
    }

    private static bool TryReadInstant(ClaimsPrincipal principal, string claimType, out DateTimeOffset instant)
    {
        instant = default;
        var value = principal.FindFirstValue(claimType);
        return long.TryParse(value, out var seconds)
            && TryFromUnixTimeSeconds(seconds, out instant);
    }

    private static bool TryFromUnixTimeSeconds(long seconds, out DateTimeOffset instant)
    {
        try
        {
            instant = DateTimeOffset.FromUnixTimeSeconds(seconds);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            instant = default;
            return false;
        }
    }

    private static string ToUnixTime(DateTimeOffset instant) => instant.ToUnixTimeSeconds().ToString();

    private static void ReplaceClaim(ClaimsIdentity identity, string type, string value)
    {
        foreach (var existing in identity.FindAll(type).ToArray())
        {
            identity.RemoveClaim(existing);
        }

        identity.AddClaim(new Claim(type, value));
    }
}
