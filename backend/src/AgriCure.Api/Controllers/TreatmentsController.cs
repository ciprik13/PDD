using AgriCure.Application.Common.Exceptions;
using AgriCure.Application.Features.Treatments;
using AgriCure.Application.Features.Treatments.Common;
using AgriCure.Domain.Detections;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriCure.Api.Controllers;

/// <summary>Treatment recommendations (catalog) and applied-treatment history.</summary>
[Authorize(Roles = "admin,agriculture")]
[Route("api/treatments")]
public sealed class TreatmentsController(
    IMediator mediator,
    ILogger<TreatmentsController> logger) : AppControllerBase(logger)
{
    /// <summary>Recommended treatments for a disease class, biological-first by rank.</summary>
    /// <param name="diseaseClass">snake_case disease class, e.g. <c>late_blight</c>.</param>
    /// <param name="cancellationToken"></param>
    /// <response code="200">Recommendations (may be empty if the class has no catalog entries).</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="403">Caller lacks the required role.</response>
    /// <response code="422">Unknown disease class.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TreatmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetByDisease(
        [FromQuery] string diseaseClass,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
        {
            var parsed = ParseDiseaseClass(diseaseClass);
            return Ok(await mediator.Send(new GetTreatmentsQuery(parsed), cancellationToken));
        });

    /// <summary>List treatment-application history, newest first. Optionally filter by plant.</summary>
    /// <param name="limit">Max records (1–200, default 50).</param>
    /// <param name="plantId">Optional plant filter.</param>
    /// <param name="cancellationToken"></param>
    /// <response code="200">Applied-treatment records (may be empty).</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="403">Caller lacks the required role.</response>
    [HttpGet("applied")]
    [ProducesResponseType(typeof(IReadOnlyList<AppliedTreatmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetApplied(
        [FromQuery] int limit = 50,
        [FromQuery] string? plantId = null,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(async () =>
            Ok(await mediator.Send(new GetAppliedTreatmentsQuery(limit, plantId), cancellationToken)));

    /// <summary>Record that a treatment was applied to a plant in the field.</summary>
    /// <response code="201">Application recorded; body has the new record id.</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="403">Caller lacks the required role.</response>
    /// <response code="422">Unknown treatment, or plant not found / not owned by the caller.</response>
    [HttpPost("applied")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> RecordApplied(
        [FromBody] RecordAppliedTreatmentCommand command,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
        {
            var id = await mediator.Send(command, cancellationToken);
            return Created(string.Empty, new { id });
        });

    // The frontend sends snake_case enum values (e.g. "late_blight"); the default model
    // binder only matches PascalCase names, so map explicitly and 422 on anything unknown.
    private static DiseaseClass ParseDiseaseClass(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            var normalized = value.Replace("_", string.Empty);
            foreach (var dc in Enum.GetValues<DiseaseClass>())
            {
                if (string.Equals(dc.ToString(), normalized, StringComparison.OrdinalIgnoreCase))
                {
                    return dc;
                }
            }
        }

        throw new UnprocessableEntityException(
            "diseaseClass",
            $"'{value}' is not a known disease class.");
    }
}
