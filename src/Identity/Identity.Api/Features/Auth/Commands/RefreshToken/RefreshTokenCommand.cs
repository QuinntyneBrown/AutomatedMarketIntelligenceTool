using MediatR;

namespace Identity.Api.Features.Auth.Commands.RefreshToken;

public sealed record RefreshTokenCommand(
    string RefreshToken) : IRequest<RefreshTokenResponse>;

public sealed record RefreshTokenResponse(
    bool Success,
    string? AccessToken,
    string? RefreshToken,
    DateTimeOffset? ExpiresAt,
    string? Error);
