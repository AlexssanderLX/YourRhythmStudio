using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Foundation.Access.Abstractions;
using Foundation.Access.Options;
using Foundation.Core.Utilities;

namespace Foundation.Access.Security;

public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private static readonly Regex UppercaseRegex = new("[A-Z]", RegexOptions.Compiled);
    private static readonly Regex LowercaseRegex = new("[a-z]", RegexOptions.Compiled);
    private static readonly Regex DigitRegex = new("[0-9]", RegexOptions.Compiled);
    private static readonly Regex SpecialRegex = new("[^a-zA-Z0-9]", RegexOptions.Compiled);

    private readonly AccessModuleOptions _options;

    public Pbkdf2PasswordHasher(AccessModuleOptions options)
    {
        _options = options;
    }

    public PasswordCredential HashPassword(string rawPassword)
    {
        var password = Guard.AgainstNullOrWhiteSpace(rawPassword, nameof(rawPassword));
        EnsurePasswordPolicy(password);

        var salt = RandomNumberGenerator.GetBytes(_options.PasswordSaltSizeBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            _options.PasswordHashIterations,
            HashAlgorithmName.SHA256,
            _options.PasswordHashSizeBytes);

        return new PasswordCredential
        {
            Iterations = _options.PasswordHashIterations,
            SaltBase64 = Convert.ToBase64String(salt),
            HashBase64 = Convert.ToBase64String(hash),
            UpdatedAtUtc = DateTime.UtcNow
        };
    }

    public PasswordVerificationResult Verify(string rawPassword, PasswordCredential credential)
    {
        var password = Guard.AgainstNullOrWhiteSpace(rawPassword, nameof(rawPassword));
        var salt = Convert.FromBase64String(credential.SaltBase64);
        var expectedHash = Convert.FromBase64String(credential.HashBase64);

        var actualHash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            credential.Iterations,
            HashAlgorithmName.SHA256,
            expectedHash.Length);

        var success = CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        var needsRehash = credential.Iterations != _options.PasswordHashIterations || expectedHash.Length != _options.PasswordHashSizeBytes;

        return new PasswordVerificationResult(success, success && needsRehash);
    }

    private void EnsurePasswordPolicy(string password)
    {
        Guard.AgainstLessThan(password.Length, _options.PasswordMinLength, nameof(password));

        if (_options.RequireUppercaseInPassword && !UppercaseRegex.IsMatch(password))
        {
            throw new ArgumentException("A senha precisa ter pelo menos uma letra maiuscula.", nameof(password));
        }

        if (_options.RequireLowercaseInPassword && !LowercaseRegex.IsMatch(password))
        {
            throw new ArgumentException("A senha precisa ter pelo menos uma letra minuscula.", nameof(password));
        }

        if (_options.RequireDigitInPassword && !DigitRegex.IsMatch(password))
        {
            throw new ArgumentException("A senha precisa ter pelo menos um numero.", nameof(password));
        }

        if (_options.RequireSpecialCharacterInPassword && !SpecialRegex.IsMatch(password))
        {
            throw new ArgumentException("A senha precisa ter pelo menos um caractere especial.", nameof(password));
        }
    }
}
