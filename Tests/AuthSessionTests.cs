using System.Security.Claims;
using Foundation.Access.Abstractions;
using Foundation.Access.Accounts;
using Foundation.Access.Models;
using Foundation.Access.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using YourRhythmStudio.Infrastructure.Auth;
using YourRhythmStudio.Infrastructure.Data;

namespace YourRhythmStudio.Tests;

public sealed class AuthSessionTests
{
    [Fact]
    public void SessionDefaults_AreShortAndBounded()
    {
        var options = new AuthSessionOptions();

        Assert.Equal(TimeSpan.FromMinutes(3), options.IdleTimeout);
        Assert.Equal(TimeSpan.FromMinutes(30), options.AbsoluteTimeout);
        Assert.Equal(TimeSpan.FromSeconds(60), options.ValidationInterval);
    }

    [Fact]
    public void CookieOptions_InProduction_AreHardened()
    {
        var cookie = new CookieAuthenticationOptions();

        YourRhythmAuthenticationExtensions.ConfigureCookie(
            cookie,
            new TestEnvironment("Production"),
            new AuthSessionOptions());

        Assert.Equal("YourRhythmStudio.Auth", cookie.Cookie.Name);
        Assert.True(cookie.Cookie.HttpOnly);
        Assert.Equal(CookieSecurePolicy.Always, cookie.Cookie.SecurePolicy);
        Assert.Equal(SameSiteMode.Lax, cookie.Cookie.SameSite);
        Assert.Equal(TimeSpan.FromMinutes(3), cookie.ExpireTimeSpan);
        Assert.True(cookie.SlidingExpiration);
        Assert.Equal(typeof(YourRhythmCookieEvents), cookie.EventsType);
    }

    [Fact]
    public async Task ValidatePrincipal_RejectsExpiredAbsoluteLifetime()
    {
        var account = ActiveAccount();
        var context = CreateContext(account, issuedAt: DateTimeOffset.UtcNow.AddMinutes(-31));
        var events = CreateEvents(account);

        await events.ValidatePrincipal(context);

        Assert.Null(context.Principal);
    }

    [Fact]
    public async Task ValidatePrincipal_RejectsSecurityStampMismatch()
    {
        var account = ActiveAccount(securityStamp: "server-stamp");
        var context = CreateContext(account, securityStamp: "cookie-stamp");
        var events = CreateEvents(account);

        await events.ValidatePrincipal(context);

        Assert.Null(context.Principal);
    }

    [Fact]
    public async Task ValidatePrincipal_RenewsRecentlyValidAccountWithinAbsoluteLifetime()
    {
        var account = ActiveAccount();
        var context = CreateContext(
            account,
            issuedAt: DateTimeOffset.UtcNow.AddMinutes(-5),
            lastValidatedAt: DateTimeOffset.UtcNow.AddMinutes(-2));
        var events = CreateEvents(account);

        await events.ValidatePrincipal(context);

        Assert.NotNull(context.Principal);
        Assert.True(context.ShouldRenew);
        Assert.NotNull(context.Principal!.FindFirst(YourRhythmAuthClaims.LastValidatedAtUtc));
    }

    [Fact]
    public async Task ValidatePrincipal_RejectsInactiveAccount()
    {
        var account = ActiveAccount();
        account.Status = AccountStatus.Suspended;
        var context = CreateContext(account);
        var events = CreateEvents(account);

        await events.ValidatePrincipal(context);

        Assert.Null(context.Principal);
    }

    private static YourRhythmCookieEvents CreateEvents(Account? account)
    {
        var dbOptions = new DbContextOptionsBuilder<YourRhythmDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new YourRhythmCookieEvents(
            Options.Create(new AuthSessionOptions()),
            new FakeAccountStore(account),
            new InMemorySessionTicketStore(),
            new YourRhythmDbContext(dbOptions),
            NullLogger<YourRhythmCookieEvents>.Instance);
    }

    private static CookieValidatePrincipalContext CreateContext(
        Account account,
        DateTimeOffset? issuedAt = null,
        DateTimeOffset? lastValidatedAt = null,
        string? securityStamp = null)
    {
        var now = DateTimeOffset.UtcNow;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new(ClaimTypes.Name, account.DisplayName),
            new(ClaimTypes.Email, account.Email),
            new("SessionId", Guid.NewGuid().ToString()),
            new(YourRhythmAuthClaims.IssuedAtUtc, (issuedAt ?? now).ToUnixTimeSeconds().ToString()),
            new(YourRhythmAuthClaims.LastValidatedAtUtc, (lastValidatedAt ?? now.AddMinutes(-2)).ToUnixTimeSeconds().ToString()),
            new(YourRhythmAuthClaims.SecurityStamp, securityStamp ?? account.SecurityStamp ?? string.Empty)
        };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            claims,
            YourRhythmAuthenticationExtensions.CookieScheme,
            ClaimTypes.Name,
            ClaimTypes.Role));

        var services = new ServiceCollection()
            .AddLogging()
            .AddAuthentication(YourRhythmAuthenticationExtensions.CookieScheme)
            .AddCookie(YourRhythmAuthenticationExtensions.CookieScheme)
            .Services
            .BuildServiceProvider();

        var httpContext = new DefaultHttpContext { RequestServices = services };
        var scheme = new AuthenticationScheme(
            YourRhythmAuthenticationExtensions.CookieScheme,
            null,
            typeof(IAuthenticationHandler));
        var ticket = new AuthenticationTicket(
            principal,
            new AuthenticationProperties(),
            YourRhythmAuthenticationExtensions.CookieScheme);

        return new CookieValidatePrincipalContext(
            httpContext,
            scheme,
            new CookieAuthenticationOptions(),
            ticket);
    }

    private static Account ActiveAccount(string securityStamp = "stamp") => new()
    {
        Id = Guid.NewGuid(),
        DisplayName = "Alexssander",
        Email = "alex@example.com",
        Status = AccountStatus.Active,
        SecurityStamp = securityStamp
    };

    private sealed class FakeAccountStore : IAccountStore
    {
        private readonly Account? _account;

        public FakeAccountStore(Account? account) => _account = account;

        public Task SaveAsync(Account account, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<Account?> FindByIdAsync(Guid accountId, CancellationToken cancellationToken = default)
            => Task.FromResult(_account?.Id == accountId ? _account : null);

        public Task<Account?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
            => Task.FromResult(_account is not null
                && string.Equals(_account.Email, email, StringComparison.OrdinalIgnoreCase)
                    ? _account
                    : null);

        public Task<bool> AnyPlatformAdministratorAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task UpdateAsync(Account account, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public TestEnvironment(string environmentName) => EnvironmentName = environmentName;

        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "YourRhythmStudio.Tests";
        public string WebRootPath { get; set; } = string.Empty;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
