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
    /// <summary>List recent detections, newest first. Requires authentication.</summary>
    /// <param name="limit">Number of detections to return (1–200, default 20).</param>
    /// <response code="200">Detections array, newest first. Empty array if none.</response>
    /// <response code="400">Validation error — `limit` must be greater than zero.</response>
    /// <response code="401">Caller is not authenticated.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DetectionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetAll(
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(async () =>
            Ok(await mediator.Send(new GetDetectionsQuery(limit), cancellationToken)));

    /// <summary>Fetch a single detection by id. Requires authentication.</summary>
    /// <response code="200">Detection found.</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="404">No detection with that id.</response>
    [HttpGet("{id:guid}", Name = nameof(GetById))]
    [ProducesResponseType(typeof(DetectionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
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

    /// <summary>Ingest a new detection. Auto-creates the referenced Plant if missing.</summary>
    /// <response code="201">Detection created. The `Location` header points to the new resource.</response>
    /// <response code="400">Validation error.</response>
    /// <response code="401">Caller is not authenticated.</response>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Create(
        [FromBody] CreateDetectionCommand command,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
        {
            var id = await mediator.Send(command, cancellationToken);
            return CreatedAtRoute(nameof(GetById), new { id }, new { id });
        });

    /// <summary>Replace a detection's contents. PUT semantics — full replacement, not patch.</summary>
    /// <response code="204">Update succeeded.</response>
    /// <response code="400">Validation error.</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="404">No detection with that id.</response>
    [HttpPut("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
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

    /// <summary>Delete a detection. Idempotent — missing ids still return 204.</summary>
    /// <response code="204">Delete succeeded (or detection was already absent).</response>
    /// <response code="401">Caller is not authenticated.</response>
    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
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
