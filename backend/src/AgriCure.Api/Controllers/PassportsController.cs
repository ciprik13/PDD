using AgriCure.Application.Features.Passports;
using AgriCure.Application.Features.Passports.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriCure.Api.Controllers;

/// <summary>Per-plant life history ("passport").</summary>
[Authorize(Roles = "admin,agriculture")]
[Route("api/passports")]
public sealed class PassportsController(
    IMediator mediator,
    ILogger<PassportsController> logger) : AppControllerBase(logger)
{
    /// <summary>
    /// Full history for one plant: identity, current status, event timeline (scans +
    /// applied treatments) and a daily severity sparkline. Agriculture users may only
    /// read passports for plants they own.
    /// </summary>
    /// <param name="plantId">Short plant code, e.g. <c>P023</c>.</param>
    /// <param name="cancellationToken"></param>
    /// <response code="200">Passport found.</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="403">Caller lacks the required role.</response>
    /// <response code="404">No such plant, or it isn't visible to the caller.</response>
    [HttpGet("{plantId}")]
    [ProducesResponseType(typeof(PlantPassportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetByPlantId(
        string plantId,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
            Ok(await mediator.Send(new GetPlantPassportQuery(plantId), cancellationToken)));
}
