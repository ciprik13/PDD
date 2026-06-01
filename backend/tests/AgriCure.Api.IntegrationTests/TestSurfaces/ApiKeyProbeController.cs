using AgriCure.Application.Common.Auth;
using AgriCure.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriCure.Api.IntegrationTests.TestSurfaces;

/// <summary>
/// Test-only controller used to exercise the ApiKey authentication handler
/// end-to-end. NOT registered in production startup; wired into MVC's
/// application parts via IntegrationTestWebAppFactory.ConfigureWebHost.
/// Will be removed once sub-project #3 ships the real /api/detections/ingest.
/// </summary>
[ApiController]
[Route("test-probe")]
public sealed class ApiKeyProbeController(ICurrentUserAccessor currentUser) : ControllerBase
{
    [HttpGet("ingest")]
    [Authorize(AuthenticationSchemes = ApiKeyAuthorization.Scheme, Policy = ApiKeyAuthorization.IngestPolicy)]
    public IActionResult Ingest() => Ok(new
    {
        userId = currentUser.UserId,
        isAdmin = currentUser.IsAdmin,
        isAgriculture = currentUser.IsAgriculture,
        isSystem = currentUser.IsSystem,
    });
}
