namespace Foundation.Access.Security;

public sealed class PasswordCredential
{
    public string Algorithm { get; init; } = "PBKDF2-SHA256";

    public int Iterations { get; init; }

    public string SaltBase64 { get; init; } = string.Empty;

    public string HashBase64 { get; init; } = string.Empty;

    public DateTime UpdatedAtUtc { get; init; }
}
