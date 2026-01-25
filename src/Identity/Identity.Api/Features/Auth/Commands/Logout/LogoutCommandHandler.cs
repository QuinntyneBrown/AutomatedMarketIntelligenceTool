using Identity.Core;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Identity.Api.Features.Auth.Commands.Logout;

public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand, LogoutResponse>
{
    private readonly IIdentityContext _context;
    private readonly ILogger<LogoutCommandHandler> _logger;

    public LogoutCommandHandler(
        IIdentityContext context,
        ILogger<LogoutCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<LogoutResponse> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(request.RefreshToken))
        {
            // Revoke specific refresh token
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == request.RefreshToken && t.UserId == request.UserId, cancellationToken);

            if (token != null && !token.IsRevoked)
            {
                token.Revoke();
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
        else
        {
            // Revoke all refresh tokens for user
            var tokens = await _context.RefreshTokens
                .Where(t => t.UserId == request.UserId && t.RevokedAt == null)
                .ToListAsync(cancellationToken);

            foreach (var token in tokens)
            {
                token.Revoke();
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("User logged out: {UserId}", request.UserId);

        return new LogoutResponse(true);
    }
}
