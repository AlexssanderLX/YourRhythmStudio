namespace Foundation.Access.Authentication;

public sealed record PasswordSignInRequest(
    string Email,
    string Password,
    string? TenantKey = null);
