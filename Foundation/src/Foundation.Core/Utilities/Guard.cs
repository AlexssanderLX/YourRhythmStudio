namespace Foundation.Core.Utilities;

public static class Guard
{
    public static string AgainstNullOrWhiteSpace(string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null or empty.", paramName);
        }

        return value.Trim();
    }

    public static decimal AgainstNegative(decimal value, string paramName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, "Value cannot be negative.");
        }

        return value;
    }

    public static int AgainstLessThan(int value, int minimum, string paramName)
    {
        if (value < minimum)
        {
            throw new ArgumentOutOfRangeException(paramName, $"Value cannot be less than {minimum}.");
        }

        return value;
    }
}
