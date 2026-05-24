using AgriCure.Application.Features.Pictures;
using AgriCure.Application.Features.Pictures.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriCure.Api.Controllers;

internal static class RouteNames
{
    public const string GetPictureById = "GetPictureById";
}

/// <summary>Picture upload, registration, lookup, and deletion.</summary>
[Authorize]
[Route("api/pictures")]
public sealed class PicturesController(
    IMediator mediator,
    ILogger<PicturesController> logger) : AppControllerBase(logger)
{
    /// <summary>Upload a new image via multipart form. The backend stores it in MinIO and creates a Picture row.</summary>
    /// <param name="form">Multipart form with a required <c>file</c> field and optional <c>alt</c>/<c>title</c>.</param>
    /// <response code="201">Picture created; the response body contains the new Picture + its public URL.</response>
    /// <response code="400">Validation error (missing file, disallowed mime type, oversize file, …).</response>
    /// <response code="401">Caller is not authenticated.</response>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(PictureDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Upload(
        [FromForm] UploadPictureForm form,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
        {
            if (form.File is null || form.File.Length == 0)
            {
                ModelState.AddModelError(nameof(form.File), "A non-empty file is required.");
                return ValidationProblem(ModelState);
            }

            using var ms = new MemoryStream();
            await form.File.CopyToAsync(ms, cancellationToken);

            var dto = await mediator.Send(
                new UploadPictureCommand(
                    ms.ToArray(),
                    form.File.ContentType,
                    string.IsNullOrWhiteSpace(form.File.FileName) ? null : Path.GetFileNameWithoutExtension(form.File.FileName),
                    form.Alt,
                    form.Title),
                cancellationToken);

            return CreatedAtRoute(RouteNames.GetPictureById, new { id = dto.Id }, dto);
        });

    /// <summary>Register a path the external sync server already uploaded to the configured bucket.</summary>
    /// <response code="201">Picture registered; response body has the new Picture + URL.</response>
    /// <response code="400">Validation error.</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="404">No object exists at the supplied <c>virtualPath</c>.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(PictureDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Register(
        [FromBody] RegisterPictureCommand command,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
        {
            var dto = await mediator.Send(command, cancellationToken);
            return CreatedAtRoute(RouteNames.GetPictureById, new { id = dto.Id }, dto);
        });

    /// <summary>Fetch a single picture's metadata + URL.</summary>
    /// <response code="200">Picture found.</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="404">No picture with that id.</response>
    [HttpGet("{id:guid}", Name = RouteNames.GetPictureById)]
    [ProducesResponseType(typeof(PictureDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
            Ok(await mediator.Send(new GetPictureQuery(id), cancellationToken)));

    /// <summary>Just the URL for a picture — useful when the caller already has the picture's id.</summary>
    /// <response code="200">URL returned.</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="404">No picture with that id.</response>
    [HttpGet("{id:guid}/url")]
    [ProducesResponseType(typeof(PictureUrlDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetUrl(
        Guid id,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
            Ok(await mediator.Send(new GetPictureUrlQuery(id), cancellationToken)));

    /// <summary>Delete a picture row and the underlying storage object.</summary>
    /// <response code="204">Deleted.</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="404">No picture with that id.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
        {
            await mediator.Send(new DeletePictureCommand(id), cancellationToken);
            return NoContent();
        });
}

/// <summary>Multipart form payload for <c>POST /api/pictures</c>.</summary>
public sealed class UploadPictureForm
{
    /// <summary>The image file. Required.</summary>
    public IFormFile? File { get; set; }

    /// <summary>HTML <c>img</c> alt attribute. Optional.</summary>
    public string? Alt { get; set; }

    /// <summary>HTML <c>img</c> title attribute. Optional.</summary>
    public string? Title { get; set; }
}
