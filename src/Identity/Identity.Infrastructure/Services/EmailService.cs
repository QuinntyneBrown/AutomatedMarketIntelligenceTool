using Identity.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Services;

/// <summary>
/// Email service implementation.
/// Note: This is a placeholder that logs emails. Replace with actual email provider (SendGrid, etc.) for production.
/// </summary>
public sealed class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task SendVerificationEmailAsync(string email, string token, CancellationToken cancellationToken = default)
    {
        var baseUrl = _configuration["App:BaseUrl"] ?? "http://localhost:5000";
        var verificationUrl = $"{baseUrl}/api/auth/verify-email?token={token}";

        _logger.LogInformation(
            "Sending verification email to {Email}. Verification URL: {VerificationUrl}",
            email,
            verificationUrl);

        // TODO: Implement actual email sending using SendGrid, SMTP, etc.
        // For development, just log the URL

        return Task.CompletedTask;
    }

    public Task SendPasswordResetEmailAsync(string email, string token, CancellationToken cancellationToken = default)
    {
        var baseUrl = _configuration["App:BaseUrl"] ?? "http://localhost:5000";
        var resetUrl = $"{baseUrl}/reset-password?token={token}";

        _logger.LogInformation(
            "Sending password reset email to {Email}. Reset URL: {ResetUrl}",
            email,
            resetUrl);

        // TODO: Implement actual email sending using SendGrid, SMTP, etc.
        // For development, just log the URL

        return Task.CompletedTask;
    }
}
