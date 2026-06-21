using Foundation.Access.Security;

namespace Foundation.Access.Accounts;

public sealed class Account
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string DisplayName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public AccountStatus Status { get; set; } = AccountStatus.PendingApproval;

    public PlatformAccessRole PlatformRole { get; set; } = PlatformAccessRole.None;

    public PasswordCredential? PasswordCredential { get; set; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? ActivatedAtUtc { get; set; }

    public DateTime? SuspendedAtUtc { get; set; }

    public string? SecurityStamp { get; set; }

    public bool IsActive => Status == AccountStatus.Active;
}
