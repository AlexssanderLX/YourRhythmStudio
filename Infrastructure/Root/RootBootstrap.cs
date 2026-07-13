using System.Security.Cryptography;
using System.Text;
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
    // Email interno fixo para a conta root (o admin pode alterar depois no painel)
    public const string RootEmail = "ROOT@YOURRHYTHM.LOCAL";

    public static async Task EnsureRootAccountAsync(IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var accountStore = sp.GetRequiredService<IAccountStore>();
        var hasher = sp.GetRequiredService<IPasswordHasher>();
        var clock = sp.GetRequiredService<IClock>();
        var logger = sp.GetRequiredService<ILogger<IAccountStore>>();

        var existing = await accountStore.FindByEmailAsync(RootEmail, ct);
        if (existing is not null) return;

        // Gera senha segura aleatória — exibida uma única vez no log
        var password = GeneratePassword();
        var now = clock.UtcNow;

        var account = new Account
        {
            DisplayName = "Root Admin",
            Email = RootEmail,
            Status = AccountStatus.Active,
            PlatformRole = PlatformAccessRole.PlatformAdmin,
            PasswordCredential = hasher.HashPassword(password),
            CreatedAtUtc = now,
            ActivatedAtUtc = now,
            SecurityStamp = Guid.NewGuid().ToString("N")
        };

        await accountStore.SaveAsync(account, ct);

        // Salva em arquivo local (não versionado)
        await WriteCredentialsFileAsync(password);

        logger.LogWarning(
            "\n" +
            "╔══════════════════════════════════════════════════════╗\n" +
            "║         CONTA ROOT CRIADA — SALVE ESTAS CREDENCIAIS  ║\n" +
            "╠══════════════════════════════════════════════════════╣\n" +
            "║  Login : root@yourrhythm.local                       ║\n" +
            "║  Senha : {Password,-52}║\n" +
            "╠══════════════════════════════════════════════════════╣\n" +
            "║  Estas credenciais aparecem UMA ÚNICA VEZ.           ║\n" +
            "║  Salvas também em: root-credentials.txt              ║\n" +
            "║  Altere-as em /Root/Settings após o primeiro login.  ║\n" +
            "╚══════════════════════════════════════════════════════╝",
            password);
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

    public static async Task EnsureDefaultLandingTracksAsync(IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<YourRhythmDbContext>();
        var environment = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

        var soundtrackDir = Path.Combine(environment.WebRootPath, "audio", "landing-soundtrack");
        var defaultTrackPath = Path.Combine(soundtrackDir, "track-1.mp3");
        if (!File.Exists(defaultTrackPath))
        {
            return;
        }

        var exists = await db.LandingTracks.AnyAsync(track => track.FileName == "track-1.mp3", ct);
        if (exists)
        {
            return;
        }

        var nextOrder = await db.LandingTracks.AnyAsync(ct)
            ? await db.LandingTracks.MaxAsync(track => track.SortOrder, ct) + 1
            : 0;

        db.LandingTracks.Add(new LandingTrack
        {
            Title = "YourRhythm Studio",
            FileName = "track-1.mp3",
            SortOrder = nextOrder,
            UploadedAtUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync(ct);
    }

    private static string GeneratePassword()
    {
        const string lower = "abcdefghjkmnpqrstuvwxyz";
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string digits = "23456789";
        const string special = "@#$!%&*";
        const string all = lower + upper + digits + special;

        var bytes = RandomNumberGenerator.GetBytes(20);
        var sb = new StringBuilder();

        // Garante pelo menos um de cada categoria
        sb.Append(lower[bytes[0] % lower.Length]);
        sb.Append(upper[bytes[1] % upper.Length]);
        sb.Append(digits[bytes[2] % digits.Length]);
        sb.Append(special[bytes[3] % special.Length]);

        for (int i = 4; i < 16; i++)
            sb.Append(all[bytes[i] % all.Length]);

        // Embaralha
        var chars = sb.ToString().ToCharArray();
        var shuffleBytes = RandomNumberGenerator.GetBytes(chars.Length);
        for (int i = chars.Length - 1; i > 0; i--)
        {
            int j = shuffleBytes[i] % (i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars);
    }

    private static async Task WriteCredentialsFileAsync(string password)
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "root-credentials.txt");
            await File.WriteAllTextAsync(path,
                $"""
                YourRhythm Studio — Credenciais iniciais da conta Root
                Geradas em: {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC

                Login : root@yourrhythm.local
                Senha : {password}

                Altere estas credenciais em /Root/Settings apos o primeiro login.
                Exclua este arquivo depois de salvar as credenciais em local seguro.
                """);
        }
        catch
        {
            // Se nao conseguir gravar o arquivo, as credenciais ainda aparecem no log
        }
    }
}
