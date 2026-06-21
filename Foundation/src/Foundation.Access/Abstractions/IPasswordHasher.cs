using Foundation.Access.Security;

namespace Foundation.Access.Abstractions;

public interface IPasswordHasher
{
    PasswordCredential HashPassword(string rawPassword);

    PasswordVerificationResult Verify(string rawPassword, PasswordCredential credential);
}
