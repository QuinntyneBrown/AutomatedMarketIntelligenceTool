using MediatR;

namespace Identity.Api.Features.Auth.Commands.Logout;

public sealed record LogoutCommand(Guid UserId, string? RefreshToken) : IRequest<LogoutResponse>;

public sealed record LogoutResponse(bool Success);
