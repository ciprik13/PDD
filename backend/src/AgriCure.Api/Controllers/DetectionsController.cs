using AgriCure.Application.Common.DTOs;
using AgriCure.Application.Features.Detections;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriCure.Api.Controllers;

[Authorize]
[Route("api/detections")]
public sealed class DetectionsController(
    IMediator mediator,
    ILogger<DetectionsController> logger) : AppControllerBase(logger)
{
    /// <summary>List recent detections, newest first. Requires `admin` or `agriculture` role.</summary>
    /// <param name="limit">Number of detections to return (1–200, default 20).</param>
    /// <response code="200">Detections array, newest first. Empty array if none.</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="403">Caller is authenticated but lacks the required role.</response>
    /// <response code="422">Validation error — `limit` must be greater than zero.</response>
    [HttpGet]
    [Authorize(Roles = "admin,agriculture")]
    [ProducesResponseType(typeof(IReadOnlyList<DetectionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetAll(
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(async () =>
            Ok(await mediator.Send(new GetDetectionsQuery(limit), cancellationToken)));

    /// <summary>Fetch a single detection by id. Requires `admin` or `agriculture` role.</summary>
    /// <response code="200">Detection found.</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="403">Caller is authenticated but lacks the required role.</response>
    /// <response code="404">No detection with that id.</response>
    [HttpGet("{id:guid}", Name = nameof(GetById))]
    [Authorize(Roles = "admin,agriculture")]
    [ProducesResponseType(typeof(DetectionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
        {
            var dto = await mediator.Send(new GetDetectionByIdQuery(id), cancellationToken);
            return dto is null ? NotFound() : Ok(dto);
        });

    /// <summary>Ingest a new detection. Auto-creates the referenced Plant if missing. Requires the `admin` role.</summary>
    /// <response code="201">Detection created. The `Location` header points to the new resource.</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="403">Caller is authenticated but lacks the admin role.</response>
    /// <response code="422">Validation error.</response>
    [HttpPost]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Create(
        [FromBody] CreateDetectionCommand command,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
        {
            var id = await mediator.Send(command, cancellationToken);
            return CreatedAtRoute(nameof(GetById), new { id }, new { id });
        });

    /// <summary>Replace a detection's contents. PUT semantics — full replacement, not patch. Requires the `admin` role.</summary>
    /// <response code="204">Update succeeded.</response>
    /// <response code="400">Route/body id mismatch.</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="403">Caller is authenticated but lacks the admin role.</response>
    /// <response code="404">No detection with that id.</response>
    /// <response code="422">Semantic validation error (field constraints).</response>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateDetectionCommand command,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
        {
            if (command.Id != id)
            {
                ModelState.AddModelError(
                    nameof(UpdateDetectionCommand.Id),
                    "Body id must match the route id.");
                return ValidationProblem(ModelState);
            }
            await mediator.Send(command, cancellationToken);
            return NoContent();
        });

    /// <summary>Delete a detection. Idempotent — missing ids still return 204. Requires the `admin` role.</summary>
    /// <response code="204">Delete succeeded (or detection was already absent).</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="403">Caller is authenticated but lacks the admin role.</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
        {
            await mediator.Send(new DeleteDetectionCommand(id), cancellationToken);
            return NoContent();
        });
}
