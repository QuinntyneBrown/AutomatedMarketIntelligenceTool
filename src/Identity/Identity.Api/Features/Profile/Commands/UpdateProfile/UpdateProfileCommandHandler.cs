using Identity.Core;
using Identity.Core.Models.UserAggregate.Events;
using Identity.Core.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Messaging.Abstractions;

namespace Identity.Api.Features.Profile.Commands.UpdateProfile;

public sealed class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, UpdateProfileResponse>
{
    private readonly IIdentityContext _context;
    private readonly IMessagePublisher _messagePublisher;
    private readonly ILogger<UpdateProfileCommandHandler> _logger;

    public UpdateProfileCommandHandler(
        IIdentityContext context,
        IMessagePublisher messagePublisher,
        ILogger<UpdateProfileCommandHandler> logger)
    {
        _context = context;
        _messagePublisher = messagePublisher;
        _logger = logger;
    }

    public async Task<UpdateProfileResponse> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        // Validate HST number if provided
        var hstValidation = ValidationService.ValidateHstNumber(request.HstNumber);
        if (!hstValidation.IsValid)
        {
            return new UpdateProfileResponse(false, hstValidation.Error);
        }

        // Validate postal code if provided
        var postalCodeValidation = ValidationService.ValidatePostalCode(request.PostalCode);
        if (!postalCodeValidation.IsValid)
        {
            return new UpdateProfileResponse(false, postalCodeValidation.Error);
        }

        // Validate phone if provided
        var phoneValidation = ValidationService.ValidatePhone(request.Phone);
        if (!phoneValidation.IsValid)
        {
            return new UpdateProfileResponse(false, phoneValidation.Error);
        }

        var profile = await _context.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == request.UserId, cancellationToken);

        if (profile == null)
        {
            return new UpdateProfileResponse(false, "Profile not found.");
        }

        // Update profile
        profile.Update(
            request.BusinessName,
            request.StreetAddress,
            request.City,
            request.Province,
            request.PostalCode,
            request.Phone,
            request.HstNumber,
            request.LogoUrl);

        await _context.SaveChangesAsync(cancellationToken);

        // Publish event
        await _messagePublisher.PublishAsync(new UserProfileUpdatedEvent
        {
            UserId = request.UserId.ToString(),
            BusinessName = request.BusinessName,
            UpdatedAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        }, cancellationToken);

        _logger.LogInformation("Profile updated for user: {UserId}", request.UserId);

        return new UpdateProfileResponse(true, null);
    }
}
