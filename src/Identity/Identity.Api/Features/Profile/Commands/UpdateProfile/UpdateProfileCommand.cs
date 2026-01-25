using MediatR;

namespace Identity.Api.Features.Profile.Commands.UpdateProfile;

public sealed record UpdateProfileCommand(
    Guid UserId,
    string? BusinessName,
    string? StreetAddress,
    string? City,
    string? Province,
    string? PostalCode,
    string? Phone,
    string? HstNumber,
    string? LogoUrl) : IRequest<UpdateProfileResponse>;

public sealed record UpdateProfileResponse(bool Success, string? Error);
