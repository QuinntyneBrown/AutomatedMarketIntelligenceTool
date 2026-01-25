using System.Text.RegularExpressions;

namespace Identity.Core.Services;

/// <summary>
/// Service for validating user input.
/// </summary>
public static partial class ValidationService
{
    /// <summary>
    /// Validates a password meets requirements (min 8 chars, uppercase, lowercase, number).
    /// </summary>
    public static (bool IsValid, string? Error) ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return (false, "Password is required.");
        }

        if (password.Length < 8)
        {
            return (false, "Password must be at least 8 characters long.");
        }

        if (!password.Any(char.IsUpper))
        {
            return (false, "Password must contain at least one uppercase letter.");
        }

        if (!password.Any(char.IsLower))
        {
            return (false, "Password must contain at least one lowercase letter.");
        }

        if (!password.Any(char.IsDigit))
        {
            return (false, "Password must contain at least one number.");
        }

        return (true, null);
    }

    /// <summary>
    /// Validates an email format.
    /// </summary>
    public static (bool IsValid, string? Error) ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return (false, "Email is required.");
        }

        if (!EmailRegex().IsMatch(email))
        {
            return (false, "Invalid email format.");
        }

        return (true, null);
    }

    /// <summary>
    /// Validates a Canadian HST number format (123456789RT0001).
    /// </summary>
    public static (bool IsValid, string? Error) ValidateHstNumber(string? hstNumber)
    {
        if (string.IsNullOrWhiteSpace(hstNumber))
        {
            return (true, null); // HST number is optional
        }

        if (!HstRegex().IsMatch(hstNumber))
        {
            return (false, "Invalid HST number format. Expected format: 123456789RT0001");
        }

        return (true, null);
    }

    /// <summary>
    /// Validates a Canadian postal code format (A1A 1A1 or A1A1A1).
    /// </summary>
    public static (bool IsValid, string? Error) ValidatePostalCode(string? postalCode)
    {
        if (string.IsNullOrWhiteSpace(postalCode))
        {
            return (true, null); // Postal code is optional
        }

        if (!PostalCodeRegex().IsMatch(postalCode))
        {
            return (false, "Invalid postal code format. Expected format: A1A 1A1 or A1A1A1");
        }

        return (true, null);
    }

    /// <summary>
    /// Validates a phone number format.
    /// </summary>
    public static (bool IsValid, string? Error) ValidatePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return (true, null); // Phone is optional
        }

        // Remove common formatting characters for validation
        var digitsOnly = new string(phone.Where(char.IsDigit).ToArray());

        if (digitsOnly.Length < 10 || digitsOnly.Length > 11)
        {
            return (false, "Invalid phone number. Expected 10 or 11 digits.");
        }

        return (true, null);
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"^\d{9}RT\d{4}$", RegexOptions.IgnoreCase)]
    private static partial Regex HstRegex();

    [GeneratedRegex(@"^[A-Za-z]\d[A-Za-z]\s?\d[A-Za-z]\d$")]
    private static partial Regex PostalCodeRegex();
}
