namespace Foundation.Access.Accounts;

public sealed record CreatePlatformAdministratorRequest(
    string DisplayName,
    string Email,
    string Password);
