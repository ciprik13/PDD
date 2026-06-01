using AgriCure.Application.Common.ApiKeys;
using AgriCure.Application.Common.Exceptions;
using FluentValidation;
using MediatR;

namespace AgriCure.Application.Features.ApiKeys;

public sealed record RevokeApiKeyCommand(Guid Id) : IRequest<Unit>;

internal sealed class RevokeApiKeyCommandValidator : AbstractValidator<RevokeApiKeyCommand>
{
    public RevokeApiKeyCommandValidator()
    {
        RuleFor(c => c.Id).NotEqual(Guid.Empty);
    }
}

internal sealed class RevokeApiKeyCommandHandler(IApiKeyService apiKeys)
    : IRequestHandler<RevokeApiKeyCommand, Unit>
{
    public async Task<Unit> Handle(RevokeApiKeyCommand request, CancellationToken cancellationToken)
    {
        var found = await apiKeys.RevokeAsync(request.Id, cancellationToken);
        if (!found)
        {
            throw new NotFoundException($"No API key with id {request.Id}.");
        }
        return Unit.Value;
    }
}
