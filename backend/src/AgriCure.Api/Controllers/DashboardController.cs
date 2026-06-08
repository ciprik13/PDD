using AgriCure.Application.Features.Dashboard;
using AgriCure.Application.Features.Dashboard.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriCure.Api.Controllers;

/// <summary>Aggregated dashboard headline stats.</summary>
[Authorize(Roles = "admin,agriculture")]
[Route("api/dashboard")]
public sealed class DashboardController(
    IMediator mediator,
    ILogger<DashboardController> logger) : AppControllerBase(logger)
{
    /// <summary>Headline numbers (detections today + delta, avg confidence, rows, plants tracked).</summary>
    /// <response code="200">Stats computed from the caller's visible data.</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="403">Caller lacks the required role.</response>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(DashboardStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetStats(CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
            Ok(await mediator.Send(new GetDashboardStatsQuery(), cancellationToken)));
}
