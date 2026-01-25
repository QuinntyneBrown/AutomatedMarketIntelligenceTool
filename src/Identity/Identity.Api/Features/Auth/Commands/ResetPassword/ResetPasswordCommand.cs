using MediatR;

namespace Identity.Api.Features.Auth.Commands.ResetPassword;

public sealed record ResetPasswordCommand(
    string Token,
    string NewPassword,
    string ConfirmPassword) : IRequest<ResetPasswordResponse>;

public sealed record ResetPasswordResponse(bool Success, string? Error);
