namespace AgriCure.Infrastructure.Auth;

/// <summary>
/// Constants for the ApiKey authentication scheme and authorization policies.
/// Kept in a dedicated file so callers (controllers, test surfaces) can reference
/// them without taking a wider dependency on the handler internals.
/// </summary>
public static class ApiKeyAuthorization
{
    /// <summary>Scheme name registered with ASP.NET Core authentication.</summary>
    public const string Scheme = "ApiKey";

    /// <summary>Header inspected by the handler.</summary>
    public const string HeaderName = "X-Api-Key";

    /// <summary>Plaintext prefix every issued key carries.</summary>
    public const string KeyPrefix = "pdd_live_";

    /// <summary>Scope value required to call the detection ingest endpoint.</summary>
    public const string IngestScope = "detections:ingest";

    /// <summary>Policy name. Use `[Authorize(AuthenticationSchemes = Scheme, Policy = IngestPolicy)]`.</summary>
    public const string IngestPolicy = "DetectionsIngest";

    /// <summary>Claim type that carries the scope on a system-authenticated principal.</summary>
    public const string ScopeClaimType = "scope";

    /// <summary>Claim type that carries the resolved ApiKey id on a system-authenticated principal.</summary>
    public const string ApiKeyIdClaimType = "api_key_id";
}
