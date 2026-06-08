using AgriCure.Application.Features.Plants;
using AgriCure.Application.Features.Plants.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriCure.Api.Controllers;

/// <summary>Plants in the field with their current disease status.</summary>
[Authorize(Roles = "admin,agriculture")]
[Route("api/plants")]
public sealed class PlantsController(
    IMediator mediator,
    ILogger<PlantsController> logger) : AppControllerBase(logger)
{
    /// <summary>
    /// List all plants the caller can see, each with its latest disease label/severity.
    /// Agriculture users see only plants they own; admins see all. Newest-seen first.
    /// </summary>
    /// <response code="200">Plants array (may be empty).</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="403">Caller lacks the required role.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PlantSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
            Ok(await mediator.Send(new GetPlantsQuery(), cancellationToken)));
}
