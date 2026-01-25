using Identity.Core;
using Identity.Core.Models.UserAggregate.Entities;
using Identity.Core.Services;
using Identity.Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Identity.Api.Features.Auth.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    private readonly IIdentityContext _context;
    private readonly ITokenService _tokenService;
    private readonly JwtOptions _jwtOptions;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IIdentityContext context,
        ITokenService tokenService,
        IOptions<JwtOptions> jwtOptions,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _context = context;
        _tokenService = tokenService;
        _jwtOptions = jwtOptions.Value;
        _logger = logger;
    }

    public async Task<RefreshTokenResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var existingToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken, cancellationToken);

        if (existingToken == null || !existingToken.IsActive)
        {
            _logger.LogWarning("Invalid or expired refresh token used");
            return new RefreshTokenResponse(false, null, null, null, "Invalid or expired refresh token.");
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == existingToken.UserId, cancellationToken);

        if (user == null || !user.IsActive)
        {
            _logger.LogWarning("Refresh token used for invalid user: {UserId}", existingToken.UserId);
            return new RefreshTokenResponse(false, null, null, null, "User not found or inactive.");
        }

        // Revoke old token
        var newRefreshTokenValue = _tokenService.GenerateRefreshToken();
        existingToken.Revoke(newRefreshTokenValue);

        // Create new refresh token
        var newRefreshToken = Core.Models.UserAggregate.Entities.RefreshToken.Create(
            user.UserId,
            newRefreshTokenValue,
            _jwtOptions.RefreshTokenLifetime);

        _context.RefreshTokens.Add(newRefreshToken);

        // Generate new access token
        var accessToken = _tokenService.GenerateAccessToken(user);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Token refreshed for user: {UserId}", user.UserId);

        var expiresAt = DateTimeOffset.UtcNow.Add(_jwtOptions.AccessTokenLifetime);

        return new RefreshTokenResponse(true, accessToken, newRefreshTokenValue, expiresAt, null);
    }
}
