using AgriCure.Application.Common.Auth;
using AgriCure.Application.Common.DTOs;
using FluentValidation;
using MediatR;

namespace AgriCure.Application.Features.Auth;

public sealed record LoginCommand(string Email, string Password) : IRequest<AuthResultDto>;

internal sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(c => c.Email).NotEmpty();
        RuleFor(c => c.Password).NotEmpty();
    }
}

internal sealed class LoginCommandHandler(
    IIdentityService identity,
    AuthTokenIssuer tokenIssuer)
    : IRequestHandler<LoginCommand, AuthResultDto>
{
    public async Task<AuthResultDto> Handle(
        LoginCommand request, CancellationToken cancellationToken)
    {
        var userContext = await identity.AuthenticateAsync(
            request.Email, request.Password, cancellationToken)
            ?? throw new AuthenticationFailedException("Invalid email or password.");

        return await tokenIssuer.IssueAsync(userContext, cancellationToken);
    }
}
