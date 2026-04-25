namespace AgriCure.Application.Common.Auth;

public interface IIdentityService
{
    Task<IdentityOperationResult> RegisterAsync(
        string email, string password, CancellationToken cancellationToken);

    Task<IdentityUserContext?> AuthenticateAsync(
        string email, string password, CancellationToken cancellationToken);

    Task<IdentityUserContext?> GetUserContextAsync(
        Guid userId, CancellationToken cancellationToken);
}

public sealed record IdentityErrorInfo(string Code, string Description);

public sealed record IdentityOperationResult(Guid? UserId, IReadOnlyList<IdentityErrorInfo> Errors)
{
    public bool Succeeded => UserId is not null && Errors.Count == 0;
}

public sealed record IdentityUserContext(Guid UserId, string Email, IReadOnlyList<string> Roles);
