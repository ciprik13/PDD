using AgriCure.Application.Common.Auth;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace AgriCure.Application.Features.Users;

/// <summary>Admin-only: create a new field user already assigned the agriculture role.</summary>
public sealed record CreateAgricultureUserCommand(string Email, string Password) : IRequest<UserDto>;

internal sealed class CreateAgricultureUserCommandValidator : AbstractValidator<CreateAgricultureUserCommand>
{
    public CreateAgricultureUserCommandValidator()
    {
        RuleFor(c => c.Email).NotEmpty().EmailAddress();
        RuleFor(c => c.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a digit.");
    }
}

internal sealed class CreateAgricultureUserCommandHandler(IIdentityService identity)
    : IRequestHandler<CreateAgricultureUserCommand, UserDto>
{
    private const string AgricultureRole = "agriculture";

    public async Task<UserDto> Handle(
        CreateAgricultureUserCommand request, CancellationToken cancellationToken)
    {
        var result = await identity.CreateUserAsync(
            request.Email, request.Password, AgricultureRole, cancellationToken);

        if (!result.Succeeded)
        {
            throw new ValidationException(MapErrors(result.Errors));
        }

        var ctx = await identity.GetUserContextAsync(result.UserId!.Value, cancellationToken)
            ?? throw new InvalidOperationException("User context unavailable after creation.");

        return new UserDto(ctx.UserId, ctx.Email, ctx.Roles);
    }

    private static ValidationFailure[] MapErrors(IReadOnlyList<IdentityErrorInfo> errors) =>
        errors.Select(e => e.Code switch
        {
            "DuplicateUserName" or "DuplicateEmail" =>
                new ValidationFailure(nameof(CreateAgricultureUserCommand.Email), "An account with this email already exists."),
            "InvalidUserName" or "InvalidEmail" =>
                new ValidationFailure(nameof(CreateAgricultureUserCommand.Email), "Email is not valid."),
            var c when c.StartsWith("Password", StringComparison.Ordinal) =>
                new ValidationFailure(nameof(CreateAgricultureUserCommand.Password), e.Description),
            _ => new ValidationFailure(string.Empty, e.Description),
        }).ToArray();
}
