using MediatR;

namespace Identity.Api.Features.Auth.Commands.Register;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string ConfirmPassword) : IRequest<RegisterResponse>;

public sealed record RegisterResponse(
    bool Success,
    string? UserId,
    string? Error);
