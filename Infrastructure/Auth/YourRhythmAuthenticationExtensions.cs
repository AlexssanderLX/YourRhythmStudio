using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace YourRhythmStudio.Infrastructure.Auth;

public static class YourRhythmAuthenticationExtensions
{
    public const string CookieScheme = "YourRhythmCookie";

    public static IServiceCollection AddYourRhythmAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.Configure<AuthSessionOptions>(configuration.GetSection(AuthSessionOptions.SectionName));
        services.AddScoped<YourRhythmCookieEvents>();

        ConfigureDataProtection(services, configuration, environment);

        services
            .AddAuthentication(CookieScheme)
            .AddCookie(CookieScheme, options =>
            {
                var sessionOptions = configuration
                    .GetSection(AuthSessionOptions.SectionName)
                    .Get<AuthSessionOptions>() ?? new AuthSessionOptions();

                ConfigureCookie(options, environment, sessionOptions);
            });

        return services;
    }

    public static void ConfigureCookie(
        CookieAuthenticationOptions options,
        IHostEnvironment environment,
        AuthSessionOptions sessionOptions)
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ReturnUrlParameter = "returnUrl";

        options.Cookie.Name = "YourRhythmStudio.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.Cookie.Path = "/";

        options.ExpireTimeSpan = sessionOptions.IdleTimeout;
        options.SlidingExpiration = true;
        options.EventsType = typeof(YourRhythmCookieEvents);
    }

    private static void ConfigureDataProtection(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var configuredPath = configuration["Authentication:DataProtectionKeysPath"];
        var keyPath = !string.IsNullOrWhiteSpace(configuredPath)
            ? configuredPath
            : environment.IsDevelopment()
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "YourRhythmStudio", "DataProtectionKeys")
                : "/var/www/yourrhythm/shared/data-protection-keys";

        services.AddDataProtection()
            .SetApplicationName("YourRhythmStudio")
            .PersistKeysToFileSystem(new DirectoryInfo(keyPath));
    }
}
