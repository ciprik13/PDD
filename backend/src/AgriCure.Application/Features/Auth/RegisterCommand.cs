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
            throw new ValidationException(MapIdentityErrors(registration.Errors));
        }

        var userContext = await identity.GetUserContextAsync(
            registration.UserId!.Value, cancellationToken)
            ?? throw new InvalidOperationException(
                "User context unavailable immediately after registration.");

        return await tokenIssuer.IssueAsync(userContext, cancellationToken);
    }

    /// <summary>
    /// Translates ASP.NET Identity errors into FluentValidation failures with friendly,
    /// PII-free messages. Codes that overlap (e.g. <c>DuplicateUserName</c> and
    /// <c>DuplicateEmail</c>, since we use email-as-username) are collapsed to a single failure.
    /// </summary>
    private static ValidationFailure[] MapIdentityErrors(IReadOnlyList<IdentityErrorInfo> errors)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var failures = new List<ValidationFailure>();

        foreach (var error in errors)
        {
            var mapped = TranslateError(error);
            var dedupKey = $"{mapped.Field}|{mapped.GroupKey}";
            if (seen.Add(dedupKey))
            {
                failures.Add(new ValidationFailure(mapped.Field, mapped.Message));
            }
        }

        return failures.ToArray();
    }

    private static (string Field, string Message, string GroupKey) TranslateError(IdentityErrorInfo error) =>
        error.Code switch
        {
            "DuplicateUserName" or "DuplicateEmail" =>
                (nameof(RegisterCommand.Email), "An account with this email already exists.", "duplicate-email"),
            "InvalidUserName" or "InvalidEmail" =>
                (nameof(RegisterCommand.Email), "Email is not valid.", "invalid-email"),
            var c when c.StartsWith("Password", StringComparison.Ordinal) =>
                (nameof(RegisterCommand.Password), error.Description, c),
            _ =>
                (string.Empty, error.Description, error.Code),
        };
}
