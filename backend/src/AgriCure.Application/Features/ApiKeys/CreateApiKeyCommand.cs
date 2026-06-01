using AgriCure.Application.Common.ApiKeys;
using AgriCure.Application.Common.Auth;
using AgriCure.Application.Common.Exceptions;
using AgriCure.Application.Features.ApiKeys.Common;
using FluentValidation;
using MediatR;

namespace AgriCure.Application.Features.ApiKeys;

public sealed record CreateApiKeyCommand(Guid OwnerUserId, string Name) : IRequest<ApiKeyCreatedDto>;

internal sealed class CreateApiKeyCommandValidator : AbstractValidator<CreateApiKeyCommand>
{
    public CreateApiKeyCommandValidator()
    {
        RuleFor(c => c.OwnerUserId).NotEqual(Guid.Empty);

        RuleFor(c => c.Name)
            .NotEmpty()
            .MaximumLength(100)
            .Matches(@"^[A-Za-z0-9._\- ]+$")
                .WithMessage("Name may only contain letters, digits, dot, underscore, hyphen, or space.");
    }
}

internal sealed class CreateApiKeyCommandHandler(
    IApiKeyService apiKeys,
    IIdentityService identity,
    ICurrentUserAccessor currentUser)
    : IRequestHandler<CreateApiKeyCommand, ApiKeyCreatedDto>
{
    public async Task<ApiKeyCreatedDto> Handle(
        CreateApiKeyCommand request,
        CancellationToken cancellationToken)
    {
        var adminId = currentUser.RequireUserId();

        var ownerIsAgriculture = await identity.UserHasRoleAsync(
            request.OwnerUserId, "agriculture", cancellationToken);
        if (!ownerIsAgriculture)
        {
            throw new UnprocessableEntityException(
                nameof(CreateApiKeyCommand.OwnerUserId),
                "Owner must be an existing user with the agriculture role.");
        }

        var existing = await apiKeys.ListAsync(request.OwnerUserId, includeRevoked: false, cancellationToken);
        if (existing.Any(k => string.Equals(k.Name, request.Name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new UnprocessableEntityException(
                nameof(CreateApiKeyCommand.Name),
                $"An active key named '{request.Name}' already exists for this user.");
        }

        return await apiKeys.IssueAsync(request.OwnerUserId, request.Name, adminId, cancellationToken);
    }
}
