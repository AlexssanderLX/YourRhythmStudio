using Foundation.Access.Accounts;
using Foundation.Access.Services;

namespace YourRhythmStudio.Infrastructure.Foundation;

public static class FoundationDemoSeeder
{
    public static async Task SeedFoundationDemoAccountAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var accessService = scope.ServiceProvider.GetRequiredService<SaasAccessService>();

        var result = await accessService.CreatePlatformAdministratorAsync(
            new CreatePlatformAdministratorRequest(
                "Admin YourRhythm",
                "admin@yourrhythm.local",
                "YourRhythm@123"));

        if (result.IsFailure &&
            result.Error?.Code is not ("conflict" or "unauthorized"))
        {
            throw new InvalidOperationException(result.Error?.Message ?? "Nao foi possivel criar o usuario demo.");
        }
    }
}
