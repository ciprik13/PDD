using System.Text.Json;
using AgriCure.Application.Common.DTOs;
using AgriCure.Application.Features.Detections;
using AgriCure.Infrastructure.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Severity = AgriCure.Domain.Detections.Severity;

namespace AgriCure.Api.Controllers;

/// <summary>
/// External-system detection ingest. Authenticated via the ApiKey scheme; requires
/// the <c>detections:ingest</c> scope. Atomic multipart upload of an image plus
/// detection metadata. Returns 201 on a fresh detection, 200 with isDuplicate=true
/// when the (plantId, frameId) pair already exists.
/// </summary>
[Route("api/detections/ingest")]
public sealed class DetectionIngestController(
    IMediator mediator,
    ILogger<DetectionIngestController> logger,
    IOptions<Microsoft.AspNetCore.Mvc.JsonOptions> jsonOptions)
    : AppControllerBase(logger)
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = jsonOptions.Value.JsonSerializerOptions;

    /// <summary>Ingest a detection event from the AI/sync server.</summary>
    /// <param name="form">Multipart form with <c>file</c> (image) and <c>metadata</c> (JSON string).</param>
    /// <param name="cancellationToken"></param>
    /// <response code="201">Detection created.</response>
    /// <response code="200">Same (plantId, frameId) was already ingested; returns the existing detection ids with <c>isDuplicate=true</c>.</response>
    /// <response code="400">Validation error on the form itself (missing file or metadata, malformed JSON).</response>
    /// <response code="401">Missing or invalid API key.</response>
    /// <response code="403">API key lacks the <c>detections:ingest</c> scope.</response>
    /// <response code="409">Plant exists but is owned by a different agriculture user.</response>
    /// <response code="422">Detection metadata failed validation (empty predictions, FrameId &lt;= 0, mime type not allowed, etc.).</response>
    [HttpPost]
    [Authorize(
        AuthenticationSchemes = ApiKeyAuthorization.Scheme,
        Policy = ApiKeyAuthorization.IngestPolicy)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(IngestDetectionResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(IngestDetectionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Ingest(
        [FromForm] IngestDetectionForm form,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
        {
            if (form.File is null || form.File.Length == 0)
            {
                ModelState.AddModelError(nameof(form.File), "Image file is required.");
                return ValidationProblem(ModelState);
            }

            if (string.IsNullOrWhiteSpace(form.Metadata))
            {
                ModelState.AddModelError(nameof(form.Metadata), "Metadata JSON is required.");
                return ValidationProblem(ModelState);
            }

            IngestMetadataDto meta;
            try
            {
                meta = JsonSerializer.Deserialize<IngestMetadataDto>(form.Metadata, _jsonSerializerOptions)
                       ?? throw new JsonException("metadata deserialized to null");
            }
            catch (JsonException ex)
            {
                ModelState.AddModelError(nameof(form.Metadata), $"Invalid JSON: {ex.Message}");
                return ValidationProblem(ModelState);
            }

            using var ms = new MemoryStream();
            await form.File.CopyToAsync(ms, cancellationToken);

            var result = await mediator.Send(
                new IngestDetectionCommand(
                    ms.ToArray(),
                    form.File.ContentType,
                    meta.FrameId,
                    meta.Timestamp,
                    meta.Severity,
                    meta.Predictions,
                    meta.BoundingBox,
                    meta.InferenceMs,
                    meta.ConfidenceGatePassed,
                    meta.Row,
                    meta.PlantId,
                    meta.PositionMeters),
                cancellationToken);

            return result.IsDuplicate ? Ok(result) : Created(string.Empty, result);
        });
}

/// <summary>Multipart form payload for <c>POST /api/detections/ingest</c>.</summary>
public sealed class IngestDetectionForm
{
    /// <summary>The detection image. Required.</summary>
    public IFormFile? File { get; set; }

    /// <summary>JSON-encoded detection metadata (see <see cref="IngestMetadataDto"/>). Required.</summary>
    public string? Metadata { get; set; }
}

/// <summary>JSON shape carried in the <c>metadata</c> form field.</summary>
public sealed record IngestMetadataDto(
    long FrameId,
    DateTimeOffset Timestamp,
    Severity Severity,
    IReadOnlyList<ClassPredictionDto> Predictions,
    BoundingBoxDto BoundingBox,
    int InferenceMs,
    bool ConfidenceGatePassed,
    int Row,
    string PlantId,
    double PositionMeters);
