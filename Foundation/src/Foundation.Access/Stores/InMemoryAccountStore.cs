using System.Collections.Concurrent;
using Foundation.Access.Abstractions;
using Foundation.Access.Accounts;

namespace Foundation.Access.Stores;

public sealed class InMemoryAccountStore : IAccountStore
{
    private readonly ConcurrentDictionary<Guid, Account> _storage = new();

    public Task SaveAsync(Account account, CancellationToken cancellationToken = default)
    {
        _storage[account.Id] = account;
        return Task.CompletedTask;
    }

    public Task<Account?> FindByIdAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        _storage.TryGetValue(accountId, out var account);
        return Task.FromResult(account);
    }

    public Task<Account?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(email);
        var account = _storage.Values.FirstOrDefault(item => NormalizeEmail(item.Email) == normalizedEmail);
        return Task.FromResult(account);
    }

    public Task<bool> AnyPlatformAdministratorAsync(CancellationToken cancellationToken = default)
    {
        var hasAny = _storage.Values.Any(item => item.PlatformRole == PlatformAccessRole.PlatformAdmin && item.Status != AccountStatus.Archived);
        return Task.FromResult(hasAny);
    }

    public Task UpdateAsync(Account account, CancellationToken cancellationToken = default)
    {
        _storage[account.Id] = account;
        return Task.CompletedTask;
    }

    private static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();
}
