using Identity.Core;
using Identity.Core.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Identity.Api.Features.Auth.Commands.ResetPassword;

public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, ResetPasswordResponse>
{
    private readonly IIdentityContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<ResetPasswordCommandHandler> _logger;

    public ResetPasswordCommandHandler(
        IIdentityContext context,
        IPasswordHasher passwordHasher,
        ILogger<ResetPasswordCommandHandler> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<ResetPasswordResponse> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        // Validate password
        var passwordValidation = ValidationService.ValidatePassword(request.NewPassword);
        if (!passwordValidation.IsValid)
        {
            return new ResetPasswordResponse(false, passwordValidation.Error);
        }

        // Validate password confirmation
        if (request.NewPassword != request.ConfirmPassword)
        {
            return new ResetPasswordResponse(false, "Passwords do not match.");
        }

        var token = await _context.PasswordResetTokens
            .FirstOrDefaultAsync(t => t.Token == request.Token, cancellationToken);

        if (token == null)
        {
            return new ResetPasswordResponse(false, "Invalid reset token.");
        }

        if (!token.IsValid)
        {
            return new ResetPasswordResponse(false, "Token has expired or already been used.");
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == token.UserId, cancellationToken);

        if (user == null)
        {
            return new ResetPasswordResponse(false, "User not found.");
        }

        // Update password
        var passwordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.UpdatePassword(passwordHash);
        token.MarkUsed();

        // Revoke all existing refresh tokens
        var existingTokens = await _context.RefreshTokens
            .Where(t => t.UserId == user.UserId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var refreshToken in existingTokens)
        {
            refreshToken.Revoke();
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Password reset completed for user: {UserId}", user.UserId);

        return new ResetPasswordResponse(true, null);
    }
}
