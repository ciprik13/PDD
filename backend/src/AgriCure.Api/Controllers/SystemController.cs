using AgriCure.Application.Features.System;
using AgriCure.Application.Features.System.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriCure.Api.Controllers;

/// <summary>Edge-device / system health for the dashboard.</summary>
[Authorize(Roles = "admin,agriculture")]
[Route("api/system")]
public sealed class SystemController(
    IMediator mediator,
    ILogger<SystemController> logger) : AppControllerBase(logger)
{
    /// <summary>Device status inferred from detection recency (online if ingest is recent).</summary>
    /// <response code="200">Current system status.</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="403">Caller lacks the required role.</response>
    [HttpGet("status")]
    [ProducesResponseType(typeof(SystemStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetStatus(CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
            Ok(await mediator.Send(new GetSystemStatusQuery(), cancellationToken)));
}
