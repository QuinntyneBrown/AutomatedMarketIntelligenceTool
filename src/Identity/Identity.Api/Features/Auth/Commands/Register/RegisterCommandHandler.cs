using Identity.Core.Models.UserAggregate;
using Identity.Core.Models.UserAggregate.Entities;
using Identity.Core.Models.UserAggregate.Events;
using Identity.Core.Services;
using Identity.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Messaging;

namespace Identity.Api.Features.Auth.Commands.Register;

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResponse>
{
    private readonly IdentityDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailService _emailService;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<RegisterCommandHandler> _logger;

    public RegisterCommandHandler(
        IdentityDbContext context,
        IPasswordHasher passwordHasher,
        IEmailService emailService,
        IEventPublisher eventPublisher,
        ILogger<RegisterCommandHandler> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<RegisterResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // Validate email format
        var emailValidation = ValidationService.ValidateEmail(request.Email);
        if (!emailValidation.IsValid)
        {
            return new RegisterResponse(false, null, emailValidation.Error);
        }

        // Validate password
        var passwordValidation = ValidationService.ValidatePassword(request.Password);
        if (!passwordValidation.IsValid)
        {
            return new RegisterResponse(false, null, passwordValidation.Error);
        }

        // Validate password confirmation
        if (request.Password != request.ConfirmPassword)
        {
            return new RegisterResponse(false, null, "Passwords do not match.");
        }

        // Check for duplicate email
        var normalizedEmail = request.Email.ToLowerInvariant().Trim();
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (existingUser != null)
        {
            return new RegisterResponse(false, null, "Email already registered.");
        }

        // Create user
        var passwordHash = _passwordHasher.HashPassword(request.Password);
        var user = User.Create(normalizedEmail, passwordHash);

        _context.Users.Add(user);

        // Create user profile
        var profile = UserProfile.Create(user.UserId);
        _context.UserProfiles.Add(profile);

        // Create email verification token
        var verificationToken = EmailVerificationToken.Create(user.UserId, TimeSpan.FromHours(24));
        _context.EmailVerificationTokens.Add(verificationToken);

        await _context.SaveChangesAsync(cancellationToken);

        // Send verification email
        await _emailService.SendVerificationEmailAsync(user.Email, verificationToken.Token, cancellationToken);

        // Publish event
        await _eventPublisher.PublishAsync(new UserRegisteredEvent
        {
            UserId = user.UserId.ToString(),
            Email = user.Email,
            RegisteredAt = DateTimeOffset.UtcNow
        }, cancellationToken);

        _logger.LogInformation("User registered: {UserId} ({Email})", user.UserId, user.Email);

        return new RegisterResponse(true, user.UserId.ToString(), null);
    }
}
