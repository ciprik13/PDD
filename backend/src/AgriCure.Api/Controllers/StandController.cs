using AgriCure.Application.Features.Telemetry;
using AgriCure.Application.Features.Telemetry.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriCure.Api.Controllers;

/// <summary>Scanning-stand position telemetry.</summary>
[Authorize(Roles = "admin,agriculture")]
[Route("api/stand")]
public sealed class StandController(
    IMediator mediator,
    ILogger<StandController> logger) : AppControllerBase(logger)
{
    /// <summary>Latest stand position, derived from the most recent detection (row + position only).</summary>
    /// <response code="200">Position (fields are null when there's no data yet).</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="403">Caller lacks the required role.</response>
    [HttpGet("position")]
    [ProducesResponseType(typeof(StandPositionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetPosition(CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
            Ok(await mediator.Send(new GetStandPositionQuery(), cancellationToken)));
}
