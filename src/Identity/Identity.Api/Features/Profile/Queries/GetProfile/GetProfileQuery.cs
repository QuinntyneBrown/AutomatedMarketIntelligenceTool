using MediatR;

namespace Identity.Api.Features.Profile.Queries.GetProfile;

public sealed record GetProfileQuery(Guid UserId) : IRequest<GetProfileResponse?>;

public sealed record GetProfileResponse(
    Guid UserId,
    string Email,
    bool IsEmailVerified,
    string? BusinessName,
    string? StreetAddress,
    string? City,
    string? Province,
    string? PostalCode,
    string? Phone,
    string? HstNumber,
    string? LogoUrl,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
