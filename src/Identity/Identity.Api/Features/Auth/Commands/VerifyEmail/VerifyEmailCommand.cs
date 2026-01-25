using MediatR;

namespace Identity.Api.Features.Auth.Commands.VerifyEmail;

public sealed record VerifyEmailCommand(string Token) : IRequest<VerifyEmailResponse>;

public sealed record VerifyEmailResponse(bool Success, string? Error);
