using Identity.Core.Models.UserAggregate.Events;
using Identity.Core.Services;
using Identity.Infrastructure.Data;
using Identity.Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Messaging;

namespace Identity.Api.Features.Auth.Commands.Login;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IdentityDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IEventPublisher _eventPublisher;
    private readonly JwtOptions _jwtOptions;
    private readonly ILogger<LoginCommandHandler> _logger;

    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public LoginCommandHandler(
        IdentityDbContext context,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IEventPublisher eventPublisher,
        IOptions<JwtOptions> jwtOptions,
        ILogger<LoginCommandHandler> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _eventPublisher = eventPublisher;
        _jwtOptions = jwtOptions.Value;
        _logger = logger;
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.ToLowerInvariant().Trim();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Login attempt for non-existent email: {Email}", normalizedEmail);
            return new LoginResponse(false, null, null, null, "Invalid email or password.");
        }

        // Check lockout
        if (user.IsLockedOut)
        {
            _logger.LogWarning("Login attempt for locked out user: {UserId}", user.UserId);
            return new LoginResponse(false, null, null, null, "Account is locked. Please try again later.");
        }

        // Check if account is active
        if (!user.IsActive)
        {
            _logger.LogWarning("Login attempt for inactive user: {UserId}", user.UserId);
            return new LoginResponse(false, null, null, null, "Account is deactivated.");
        }

        // Verify password
        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            user.RecordFailedLogin(MaxFailedAttempts, LockoutDuration);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogWarning("Failed login attempt for user: {UserId}", user.UserId);
            return new LoginResponse(false, null, null, null, "Invalid email or password.");
        }

        // Successful login
        user.RecordLogin();

        // Generate tokens
        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshTokenValue = _tokenService.GenerateRefreshToken();

        var refreshToken = Identity.Core.Models.UserAggregate.Entities.RefreshToken.Create(
            user.UserId,
            refreshTokenValue,
            _jwtOptions.RefreshTokenLifetime);

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Publish event
        await _eventPublisher.PublishAsync(new UserLoggedInEvent
        {
            UserId = user.UserId.ToString(),
            LoggedInAt = DateTimeOffset.UtcNow
        }, cancellationToken);

        _logger.LogInformation("User logged in: {UserId}", user.UserId);

        var expiresAt = DateTimeOffset.UtcNow.Add(_jwtOptions.AccessTokenLifetime);

        return new LoginResponse(true, accessToken, refreshTokenValue, expiresAt, null);
    }
}
