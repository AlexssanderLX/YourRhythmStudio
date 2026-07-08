using Foundation.Access.Abstractions;
using Foundation.Access.Accounts;
using Foundation.Access.Security;
using Microsoft.EntityFrameworkCore;
using YourRhythmStudio.Infrastructure.Data;

namespace YourRhythmStudio.Infrastructure.Foundation;

/// <summary>
/// Substitui InMemoryAccountStore: mantém cache em memória para velocidade,
/// mas persiste tudo no MySQL para que contas sobrevivam a reinicializações.
/// </summary>
public sealed class MySqlBackedAccountStore : IAccountStore
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Dictionary<Guid, Account> _byId = new();
    private readonly Dictionary<string, Account> _byEmail = new();
    private bool _loaded;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public MySqlBackedAccountStore(IServiceScopeFactory scopeFactory)
        => _scopeFactory = scopeFactory;

    public async Task SaveAsync(Account account, CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        lock (_byId)
        {
            _byId[account.Id] = account;
            _byEmail[account.Email] = account;
        }
        await PersistAsync(account, ct);
    }

    public async Task UpdateAsync(Account account, CancellationToken ct = default)
        => await SaveAsync(account, ct);

    public async Task<Account?> FindByIdAsync(Guid id, CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        lock (_byId) { return _byId.GetValueOrDefault(id); }
    }

    public async Task<Account?> FindByEmailAsync(string email, CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        var key = email.Trim().ToUpperInvariant();
        lock (_byId) { return _byEmail.GetValueOrDefault(key); }
    }

    public async Task<bool> AnyPlatformAdministratorAsync(CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        lock (_byId) { return _byId.Values.Any(a => a.PlatformRole == PlatformAccessRole.PlatformAdmin); }
    }

    private async Task EnsureLoadedAsync(CancellationToken ct)
    {
        if (_loaded) return;
        await _gate.WaitAsync(ct);
        try
        {
            if (_loaded) return;
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<YourRhythmDbContext>();
            var rows = await db.PersistedAccounts.AsNoTracking().ToListAsync(ct);
            foreach (var row in rows)
            {
                var acc = ToAccount(row);
                _byId[acc.Id] = acc;
                _byEmail[acc.Email] = acc;
            }
            _loaded = true;
        }
        finally { _gate.Release(); }
    }

    private async Task PersistAsync(Account account, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<YourRhythmDbContext>();
        var existing = await db.PersistedAccounts.FindAsync(new object[] { account.Id }, ct);
        if (existing is null)
            db.PersistedAccounts.Add(ToRow(account));
        else
            UpdateRow(existing, account);
        await db.SaveChangesAsync(ct);
    }

    private static Account ToAccount(PersistedAccount r) => new()
    {
        Id = r.Id,
        DisplayName = r.DisplayName,
        Email = r.Email,
        Status = Enum.Parse<AccountStatus>(r.Status),
        PlatformRole = Enum.Parse<PlatformAccessRole>(r.PlatformRole),
        PasswordCredential = r.PwdHashBase64 is null ? null : new PasswordCredential
        {
            Algorithm = r.PwdAlgorithm ?? "PBKDF2-SHA256",
            Iterations = r.PwdIterations,
            SaltBase64 = r.PwdSaltBase64 ?? string.Empty,
            HashBase64 = r.PwdHashBase64,
            UpdatedAtUtc = r.PwdUpdatedAtUtc ?? r.CreatedAtUtc
        },
        CreatedAtUtc = r.CreatedAtUtc,
        ActivatedAtUtc = r.ActivatedAtUtc,
        SecurityStamp = r.SecurityStamp
    };

    private static PersistedAccount ToRow(Account a) => new()
    {
        Id = a.Id,
        DisplayName = a.DisplayName,
        Email = a.Email,
        Status = a.Status.ToString(),
        PlatformRole = a.PlatformRole.ToString(),
        PwdAlgorithm = a.PasswordCredential?.Algorithm,
        PwdIterations = a.PasswordCredential?.Iterations ?? 0,
        PwdSaltBase64 = a.PasswordCredential?.SaltBase64,
        PwdHashBase64 = a.PasswordCredential?.HashBase64,
        PwdUpdatedAtUtc = a.PasswordCredential?.UpdatedAtUtc,
        CreatedAtUtc = a.CreatedAtUtc,
        ActivatedAtUtc = a.ActivatedAtUtc,
        SecurityStamp = a.SecurityStamp
    };

    private static void UpdateRow(PersistedAccount r, Account a)
    {
        r.DisplayName = a.DisplayName;
        r.Email = a.Email;
        r.Status = a.Status.ToString();
        r.PlatformRole = a.PlatformRole.ToString();
        r.PwdAlgorithm = a.PasswordCredential?.Algorithm;
        r.PwdIterations = a.PasswordCredential?.Iterations ?? 0;
        r.PwdSaltBase64 = a.PasswordCredential?.SaltBase64;
        r.PwdHashBase64 = a.PasswordCredential?.HashBase64;
        r.PwdUpdatedAtUtc = a.PasswordCredential?.UpdatedAtUtc;
        r.ActivatedAtUtc = a.ActivatedAtUtc;
        r.SecurityStamp = a.SecurityStamp;
    }
}
