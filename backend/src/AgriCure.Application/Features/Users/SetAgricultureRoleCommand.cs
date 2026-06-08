using AgriCure.Application.Common.Auth;
using AgriCure.Application.Common.Exceptions;
using FluentValidation;
using MediatR;

namespace AgriCure.Application.Features.Users;

/// <summary>Admin-only: grant or revoke the agriculture role for an existing user.</summary>
public sealed record SetAgricultureRoleCommand(Guid UserId, bool Assigned) : IRequest;

internal sealed class SetAgricultureRoleCommandValidator : AbstractValidator<SetAgricultureRoleCommand>
{
    public SetAgricultureRoleCommandValidator()
    {
        RuleFor(c => c.UserId).NotEqual(Guid.Empty);
    }
}

internal sealed class SetAgricultureRoleCommandHandler(IIdentityService identity)
    : IRequestHandler<SetAgricultureRoleCommand>
{
    private const string AgricultureRole = "agriculture";

    public async Task Handle(SetAgricultureRoleCommand request, CancellationToken cancellationToken)
    {
        var found = await identity.SetRoleAsync(
            request.UserId, AgricultureRole, request.Assigned, cancellationToken);

        if (!found)
        {
            throw new NotFoundException($"User '{request.UserId}' was not found.");
        }
    }
}
