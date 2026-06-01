using AgriCure.Application.Common.ApiKeys;
using AgriCure.Application.Features.ApiKeys.Common;
using MediatR;

namespace AgriCure.Application.Features.ApiKeys;

public sealed record GetApiKeysQuery(Guid? OwnerUserId, bool IncludeRevoked)
    : IRequest<IReadOnlyList<ApiKeyDto>>;

internal sealed class GetApiKeysQueryHandler(IApiKeyService apiKeys)
    : IRequestHandler<GetApiKeysQuery, IReadOnlyList<ApiKeyDto>>
{
    public Task<IReadOnlyList<ApiKeyDto>> Handle(
        GetApiKeysQuery request,
        CancellationToken cancellationToken) =>
        apiKeys.ListAsync(request.OwnerUserId, request.IncludeRevoked, cancellationToken);
}
