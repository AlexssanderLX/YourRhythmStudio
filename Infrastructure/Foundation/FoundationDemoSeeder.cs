using Foundation.Access.Abstractions;
using Foundation.Access.Accounts;
using Foundation.Access.Security;
using Foundation.Access.Services;
using Foundation.Core.Abstractions;
using Foundation.Core.Utilities;

namespace YourRhythmStudio.Infrastructure.Foundation;

public static class FoundationDemoSeeder
{
    public static async Task SeedFoundationDemoAccountAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var accessService = scope.ServiceProvider.GetRequiredService<SaasAccessService>();

        // Admin da plataforma (fluxo oficial da Foundation).
        var result = await accessService.CreatePlatformAdministratorAsync(
            new CreatePlatformAdministratorRequest(
                "Admin YourRhythm",
                "admin@yourrhythm.local",
                DemoPersonas.DemoPassword));

        if (result.IsFailure &&
            result.Error?.Code is not ("conflict" or "unauthorized"))
        {
            throw new InvalidOperationException(result.Error?.Message ?? "Nao foi possivel criar o usuario demo.");
        }

        // Contas demo (escola / professor / aluno) para visualizar cada dashboard.
        // Semeadas direto no store in-memory: são contas comuns (PlatformRole = None)
        // e o papel de domínio é resolvido pelo e-mail em DemoPersonas.
        var accountStore = scope.ServiceProvider.GetRequiredService<IAccountStore>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        foreach (var persona in DemoPersonas.All)
        {
            if (await accountStore.FindByEmailAsync(persona.Email) is not null)
            {
                continue;
            }

            var now = clock.UtcNow;
            var account = new Account
            {
                DisplayName = persona.DisplayName,
                Email = persona.Email.ToUpperInvariant(),
                Status = AccountStatus.Active,
                PlatformRole = PlatformAccessRole.None,
                PasswordCredential = passwordHasher.HashPassword(DemoPersonas.DemoPassword),
                CreatedAtUtc = now,
                ActivatedAtUtc = now,
                SecurityStamp = SecureCodeGenerator.GenerateToken(16)
            };

            await accountStore.SaveAsync(account);
        }
    }
}
