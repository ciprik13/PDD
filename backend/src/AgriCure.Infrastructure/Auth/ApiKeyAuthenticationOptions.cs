using Microsoft.AspNetCore.Authentication;

namespace AgriCure.Infrastructure.Auth;

public sealed class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    // Intentionally empty. The handler reads everything it needs from the
    // ApiKeyAuthorization constants and the request itself.
}
