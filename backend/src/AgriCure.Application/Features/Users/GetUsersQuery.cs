using AgriCure.Application.Common.Auth;
using MediatR;

namespace AgriCure.Application.Features.Users;

/// <summary>Admin-only user listing, optionally filtered to a role (e.g. <c>agriculture</c>).</summary>
public sealed record GetUsersQuery(string? Role) : IRequest<IReadOnlyList<UserDto>>;

/// <summary>Metadata for a user account. No credentials.</summary>
public sealed record UserDto(Guid Id, string Email, IReadOnlyList<string> Roles);

internal sealed class GetUsersQueryHandler(IIdentityService identity)
    : IRequestHandler<GetUsersQuery, IReadOnlyList<UserDto>>
{
    public async Task<IReadOnlyList<UserDto>> Handle(
        GetUsersQuery request, CancellationToken cancellationToken)
    {
        var users = await identity.ListUsersAsync(request.Role, cancellationToken);
        return users.Select(u => new UserDto(u.UserId, u.Email, u.Roles)).ToArray();
    }
}
