namespace AgriCure.Application.Common.Auth;

public interface IIdentityService
{
    Task<IdentityOperationResult> RegisterAsync(
        string email, string password, CancellationToken cancellationToken);

    Task<IdentityUserContext?> AuthenticateAsync(
        string email, string password, CancellationToken cancellationToken);

    Task<IdentityUserContext?> GetUserContextAsync(
        Guid userId, CancellationToken cancellationToken);

    Task<bool> UserHasRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken);

    /// <summary>
    /// Lists users, optionally filtered to a single role. Used by the admin surface to
    /// pick an API-key owner without typing a raw user id.
    /// </summary>
    Task<IReadOnlyList<IdentityUserContext>> ListUsersAsync(
        string? roleName, CancellationToken cancellationToken);
}

public sealed record IdentityErrorInfo(string Code, string Description);

public sealed record IdentityOperationResult(Guid? UserId, IReadOnlyList<IdentityErrorInfo> Errors)
{
    public bool Succeeded => UserId is not null && Errors.Count == 0;
}

public sealed record IdentityUserContext(Guid UserId, string Email, IReadOnlyList<string> Roles);
