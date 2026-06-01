using AgriCure.Application.Features.ApiKeys;
using AgriCure.Application.Features.ApiKeys.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriCure.Api.Controllers;

/// <summary>
/// Admin-only management of system API keys. JWT auth (existing scheme); requires the admin role.
/// </summary>
[Authorize(Roles = "admin")]
[Route("api/admin/api-keys")]
public sealed class AdminApiKeysController(
    IMediator mediator,
    ILogger<AdminApiKeysController> logger) : AppControllerBase(logger)
{
    /// <summary>Create a new API key for an agriculture user. Returns the plaintext exactly once.</summary>
    /// <param name="command">Owner and human label.</param>
    /// <param name="cancellationToken"></param>
    /// <response code="201">Key created. The response body contains the plaintext key — the only time it is ever returned.</response>
    /// <response code="400">Validation error (empty/invalid name).</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="403">Caller lacks the admin role.</response>
    /// <response code="422">Owner is not an existing agriculture user, or duplicate active name for the same owner.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiKeyCreatedDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Create(
        [FromBody] CreateApiKeyCommand command,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
        {
            var dto = await mediator.Send(command, cancellationToken);
            return CreatedAtRoute(
                "AdminApiKeys_GetById",
                new { id = dto.Id },
                dto);
        });

    /// <summary>List API keys. Admin only.</summary>
    /// <param name="ownerUserId">Optional filter — only keys for this user.</param>
    /// <param name="includeRevoked">Include revoked keys in the result (default false).</param>
    /// <param name="cancellationToken"></param>
    /// <response code="200">Keys, newest-first by creation time.</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="403">Caller lacks the admin role.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ApiKeyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetAll(
        [FromQuery] Guid? ownerUserId,
        [FromQuery] bool includeRevoked = false,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(async () =>
            Ok(await mediator.Send(new GetApiKeysQuery(ownerUserId, includeRevoked), cancellationToken)));

    /// <summary>Fetch a single key's metadata. Plaintext is never returned.</summary>
    /// <param name="id">Key identifier.</param>
    /// <param name="cancellationToken"></param>
    /// <response code="200">Found.</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="403">Caller lacks the admin role.</response>
    /// <response code="404">No key with that id.</response>
    [HttpGet("{id:guid}", Name = "AdminApiKeys_GetById")]
    [ProducesResponseType(typeof(ApiKeyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
            Ok(await mediator.Send(new GetApiKeyByIdQuery(id), cancellationToken)));

    /// <summary>Revoke a key (soft-delete). Idempotent.</summary>
    /// <param name="id">Key identifier.</param>
    /// <param name="cancellationToken"></param>
    /// <response code="204">Revoked, or already revoked.</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="403">Caller lacks the admin role.</response>
    /// <response code="404">No key with that id.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
        {
            await mediator.Send(new RevokeApiKeyCommand(id), cancellationToken);
            return NoContent();
        });
}
