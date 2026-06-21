using Foundation.Access.Accounts;

namespace Foundation.Access.Abstractions;

public interface IAccountStore
{
    Task SaveAsync(Account account, CancellationToken cancellationToken = default);

    Task<Account?> FindByIdAsync(Guid accountId, CancellationToken cancellationToken = default);

    Task<Account?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> AnyPlatformAdministratorAsync(CancellationToken cancellationToken = default);

    Task UpdateAsync(Account account, CancellationToken cancellationToken = default);
}
