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

    /// <summary>Creates a user and assigns them a single role. Admin-driven onboarding.</summary>
    Task<IdentityOperationResult> CreateUserAsync(
        string email, string password, string roleName, CancellationToken cancellationToken);

    /// <summary>
    /// Adds or removes a role for an existing user. Returns <c>false</c> if the user
    /// doesn't exist. Idempotent — assigning a role the user already has is a no-op.
    /// </summary>
    Task<bool> SetRoleAsync(
        Guid userId, string roleName, bool assigned, CancellationToken cancellationToken);
}

public sealed record IdentityErrorInfo(string Code, string Description);

public sealed record IdentityOperationResult(Guid? UserId, IReadOnlyList<IdentityErrorInfo> Errors)
{
    public bool Succeeded => UserId is not null && Errors.Count == 0;
}

public sealed record IdentityUserContext(Guid UserId, string Email, IReadOnlyList<string> Roles);
