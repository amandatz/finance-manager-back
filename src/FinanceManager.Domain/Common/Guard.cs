using FinanceManager.Domain.Exceptions;

namespace FinanceManager.Domain.Common;

internal static class Guard
{
    public static void AgainstEmptyOrTooLong(string value, int maxLength, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException($"{Capitalize(paramName)} is required.", paramName);
        }

        if (value.Length > maxLength)
        {
            throw new DomainValidationException(
                $"{Capitalize(paramName)} must not exceed {maxLength} characters.",
                paramName);
        }
    }

    private static string Capitalize(string value) =>
        value.Length == 0 ? value : char.ToUpperInvariant(value[0]) + value[1..];
}
