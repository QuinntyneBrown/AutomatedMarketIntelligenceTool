using Identity.Core.Models.UserAggregate;

namespace Identity.Core.Services;

/// <summary>
/// Interface for JWT token operations.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates a JWT access token for the user.
    /// </summary>
    string GenerateAccessToken(User user);

    /// <summary>
    /// Generates a refresh token.
    /// </summary>
    string GenerateRefreshToken();

    /// <summary>
    /// Validates an access token and returns the user ID if valid.
    /// </summary>
    Guid? ValidateAccessToken(string token);
}
