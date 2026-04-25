using AgriCure.Application.Common.Auth;
using AgriCure.Application.Common.DTOs;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace AgriCure.Application.Features.Auth;

public sealed record RegisterCommand(string Email, string Password) : IRequest<AuthResultDto>;

internal sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(c => c.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(c => c.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a digit.");
    }
}

internal sealed class RegisterCommandHandler(
    IIdentityService identity,
    AuthTokenIssuer tokenIssuer)
    : IRequestHandler<RegisterCommand, AuthResultDto>
{
    public async Task<AuthResultDto> Handle(
        RegisterCommand request, CancellationToken cancellationToken)
    {
        var registration = await identity.RegisterAsync(
            request.Email, request.Password, cancellationToken);

        if (!registration.Succeeded)
        {
            var failures = registration.Errors
                .Select(error => new ValidationFailure(MapErrorField(error.Code), error.Description))
                .ToArray();
            throw new ValidationException(failures);
        }

        var userContext = await identity.GetUserContextAsync(
            registration.UserId!.Value, cancellationToken)
            ?? throw new InvalidOperationException(
                "User context unavailable immediately after registration.");

        return await tokenIssuer.IssueAsync(userContext, cancellationToken);
    }

    /// <summary>
    /// Maps ASP.NET Identity error codes to the form field they belong to,
    /// so the frontend can render the message under the right input.
    /// </summary>
    private static string MapErrorField(string code) => code switch
    {
        "DuplicateUserName" or "DuplicateEmail" or "InvalidUserName" or "InvalidEmail" =>
            nameof(RegisterCommand.Email),
        var c when c.StartsWith("Password", StringComparison.Ordinal) =>
            nameof(RegisterCommand.Password),
        _ => string.Empty,
    };
}
