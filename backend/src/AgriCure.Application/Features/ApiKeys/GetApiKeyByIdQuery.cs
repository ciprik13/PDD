using AgriCure.Application.Common.ApiKeys;
using AgriCure.Application.Common.Exceptions;
using AgriCure.Application.Features.ApiKeys.Common;
using FluentValidation;
using MediatR;

namespace AgriCure.Application.Features.ApiKeys;

public sealed record GetApiKeyByIdQuery(Guid Id) : IRequest<ApiKeyDto>;

internal sealed class GetApiKeyByIdQueryValidator : AbstractValidator<GetApiKeyByIdQuery>
{
    public GetApiKeyByIdQueryValidator()
    {
        RuleFor(q => q.Id).NotEqual(Guid.Empty);
    }
}

internal sealed class GetApiKeyByIdQueryHandler(IApiKeyService apiKeys)
    : IRequestHandler<GetApiKeyByIdQuery, ApiKeyDto>
{
    public async Task<ApiKeyDto> Handle(GetApiKeyByIdQuery request, CancellationToken cancellationToken)
    {
        var dto = await apiKeys.GetByIdAsync(request.Id, cancellationToken);
        return dto ?? throw new NotFoundException($"No API key with id {request.Id}.");
    }
}
