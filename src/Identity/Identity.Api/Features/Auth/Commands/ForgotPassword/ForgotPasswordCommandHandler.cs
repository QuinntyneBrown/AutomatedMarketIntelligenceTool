using Identity.Core;
using Identity.Core.Models.UserAggregate.Entities;
using Identity.Core.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Identity.Api.Features.Auth.Commands.ForgotPassword;

public sealed class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, ForgotPasswordResponse>
{
    private readonly IIdentityContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;

    public ForgotPasswordCommandHandler(
        IIdentityContext context,
        IEmailService emailService,
        ILogger<ForgotPasswordCommandHandler> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<ForgotPasswordResponse> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.ToLowerInvariant().Trim();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        // Always return success to prevent email enumeration
        if (user == null)
        {
            _logger.LogWarning("Password reset requested for non-existent email: {Email}", normalizedEmail);
            return new ForgotPasswordResponse(true, "If the email exists, a password reset link has been sent.");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Password reset requested for inactive user: {UserId}", user.UserId);
            return new ForgotPasswordResponse(true, "If the email exists, a password reset link has been sent.");
        }

        // Create password reset token (valid for 1 hour)
        var resetToken = PasswordResetToken.Create(user.UserId, TimeSpan.FromHours(1));
        _context.PasswordResetTokens.Add(resetToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Send reset email
        await _emailService.SendPasswordResetEmailAsync(user.Email, resetToken.Token, cancellationToken);

        _logger.LogInformation("Password reset token generated for user: {UserId}", user.UserId);

        return new ForgotPasswordResponse(true, "If the email exists, a password reset link has been sent.");
    }
}
