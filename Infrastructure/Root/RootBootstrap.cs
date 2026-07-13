using Microsoft.EntityFrameworkCore;
using Foundation.Access.Abstractions;
using Foundation.Access.Accounts;
using Foundation.Access.Security;
using Foundation.Core.Abstractions;
using YourRhythmStudio.Domain.Root;
using YourRhythmStudio.Infrastructure.Data;

namespace YourRhythmStudio.Infrastructure.Root;

public static class RootBootstrap
{
    public static async Task EnsureRootAccountAsync(IServiceProvider services, CancellationToken ct = default)
    {
        var config = services.GetRequiredService<IConfiguration>();
        var rootEmail = config["YOURRHYTHM_ROOT_EMAIL"] ?? Environment.GetEnvironmentVariable("YOURRHYTHM_ROOT_EMAIL");
        var rootPassword = config["YOURRHYTHM_ROOT_PASSWORD"] ?? Environment.GetEnvironmentVariable("YOURRHYTHM_ROOT_PASSWORD");
        var rootName = config["YOURRHYTHM_ROOT_DISPLAY_NAME"]
                       ?? Environment.GetEnvironmentVariable("YOURRHYTHM_ROOT_DISPLAY_NAME")
                       ?? "Root Admin";

        if (string.IsNullOrWhiteSpace(rootEmail) || string.IsNullOrWhiteSpace(rootPassword))
        {
            var logger = services.GetRequiredService<ILogger<IAccountStore>>();
            logger.LogWarning("Conta Root nao configurada. Defina YOURRHYTHM_ROOT_EMAIL e YOURRHYTHM_ROOT_PASSWORD para criar a conta Root.");
            return;
        }

        var accountStore = services.GetRequiredService<IAccountStore>();
        var hasher = services.GetRequiredService<IPasswordHasher>();
        var clock = services.GetRequiredService<IClock>();

        var emailNorm = rootEmail.Trim().ToUpperInvariant();
        var existing = await accountStore.FindByEmailAsync(emailNorm, ct);

        if (existing is not null)
        {
            if (existing.PlatformRole != PlatformAccessRole.PlatformAdmin)
            {
                var logger = services.GetRequiredService<ILogger<IAccountStore>>();
                logger.LogWarning("Conta {Email} existe mas nao e PlatformAdmin. Bootstrap Root ignorado.", rootEmail);
            }
            return;
        }

        var now = clock.UtcNow;
        var account = new Account
        {
            DisplayName = rootName,
            Email = emailNorm,
            Status = AccountStatus.Active,
            PlatformRole = PlatformAccessRole.PlatformAdmin,
            PasswordCredential = hasher.HashPassword(rootPassword),
            CreatedAtUtc = now,
            ActivatedAtUtc = now,
            SecurityStamp = Guid.NewGuid().ToString("N")
        };

        await accountStore.SaveAsync(account, ct);

        var logger2 = services.GetRequiredService<ILogger<IAccountStore>>();
        logger2.LogInformation("Conta Root criada com sucesso para {Email}.", rootEmail);
    }

    public static async Task EnsureDefaultPlansAsync(IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<YourRhythmDbContext>();

        if (await db.Plans.AnyAsync(ct))
            return;

        db.Plans.AddRange(
            new Plan
            {
                Code = "professor",
                Name = "Professor",
                Description = "Ideal para professores independentes. Ate 30 alunos.",
                MonthlyPriceBrl = 149.90m,
                MaxStudents = 30,
                StorageQuotaBytes = 5L * 1024 * 1024 * 1024,
                IsActive = true
            },
            new Plan
            {
                Code = "escola",
                Name = "Escola",
                Description = "Para escolas e studios. Multiplos professores, alunos ilimitados.",
                MonthlyPriceBrl = 249.90m,
                MaxStudents = null,
                StorageQuotaBytes = 20L * 1024 * 1024 * 1024,
                IsActive = true
            });

        await db.SaveChangesAsync(ct);
    }
}
