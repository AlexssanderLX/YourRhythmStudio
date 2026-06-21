using System.Security.Cryptography;
using System.Text;

namespace Foundation.Core.Utilities;

public static class SecureCodeGenerator
{
    private const string Digits = "0123456789";
    private const string AlphaNumeric = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static string GenerateDigits(int length) => GenerateFromAlphabet(Digits, length);

    public static string GenerateAlphaNumeric(int length) => GenerateFromAlphabet(AlphaNumeric, length);

    public static string GenerateToken(int sizeInBytes = 32)
    {
        var bytes = RandomNumberGenerator.GetBytes(sizeInBytes);
        return Convert.ToHexString(bytes);
    }

    public static string ComputeSha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }

    private static string GenerateFromAlphabet(string alphabet, int length)
    {
        Guard.AgainstLessThan(length, 1, nameof(length));

        var chars = new char[length];
        var bytes = RandomNumberGenerator.GetBytes(length);

        for (var i = 0; i < length; i++)
        {
            chars[i] = alphabet[bytes[i] % alphabet.Length];
        }

        return new string(chars);
    }
}
