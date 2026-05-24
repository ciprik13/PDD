namespace AgriCure.Application.Common.Auth;

/// <summary>
/// Reads identity information about the request's authenticated caller from
/// the current <c>HttpContext</c>. Returns <c>null</c>/<c>false</c> for anonymous
/// callers — handlers that require a user should call <see cref="RequireUserId"/>.
/// </summary>
public interface ICurrentUserAccessor
{
    Guid? UserId { get; }

    bool IsAuthenticated { get; }

    bool IsAdmin { get; }

    bool IsAgriculture { get; }

    /// <summary>
    /// Returns <see cref="UserId"/> or throws <see cref="AuthenticationFailedException"/>
    /// if <see cref="UserId"/> is <c>null</c> (anonymous caller OR missing/invalid <c>sub</c> claim).
    /// </summary>
    Guid RequireUserId();
}
