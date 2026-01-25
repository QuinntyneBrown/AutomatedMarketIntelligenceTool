using Identity.Core.Models.UserAggregate.Events;
using Identity.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Messaging;

namespace Identity.Api.Features.Auth.Commands.VerifyEmail;

public sealed class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, VerifyEmailResponse>
{
    private readonly IdentityDbContext _context;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<VerifyEmailCommandHandler> _logger;

    public VerifyEmailCommandHandler(
        IdentityDbContext context,
        IEventPublisher eventPublisher,
        ILogger<VerifyEmailCommandHandler> logger)
    {
        _context = context;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<VerifyEmailResponse> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        var token = await _context.EmailVerificationTokens
            .FirstOrDefaultAsync(t => t.Token == request.Token, cancellationToken);

        if (token == null)
        {
            return new VerifyEmailResponse(false, "Invalid verification token.");
        }

        if (!token.IsValid)
        {
            return new VerifyEmailResponse(false, "Token has expired or already been used.");
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == token.UserId, cancellationToken);

        if (user == null)
        {
            return new VerifyEmailResponse(false, "User not found.");
        }

        if (user.IsEmailVerified)
        {
            return new VerifyEmailResponse(false, "Email is already verified.");
        }

        // Mark email as verified
        user.VerifyEmail();
        token.MarkUsed();

        await _context.SaveChangesAsync(cancellationToken);

        // Publish event
        await _eventPublisher.PublishAsync(new UserEmailVerifiedEvent
        {
            UserId = user.UserId.ToString(),
            Email = user.Email,
            VerifiedAt = DateTimeOffset.UtcNow
        }, cancellationToken);

        _logger.LogInformation("Email verified for user: {UserId}", user.UserId);

        return new VerifyEmailResponse(true, null);
    }
}
