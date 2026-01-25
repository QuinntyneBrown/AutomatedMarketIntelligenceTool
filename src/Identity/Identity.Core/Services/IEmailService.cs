namespace Identity.Core.Services;

/// <summary>
/// Interface for email sending operations.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends a verification email.
    /// </summary>
    Task SendVerificationEmailAsync(string email, string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a password reset email.
    /// </summary>
    Task SendPasswordResetEmailAsync(string email, string token, CancellationToken cancellationToken = default);
}
