using AgriCure.Application.Features.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriCure.Api.Controllers;

/// <summary>Admin-only user lookup, used to pick an API-key owner.</summary>
[Authorize(Roles = "admin")]
[Route("api/admin/users")]
public sealed class AdminUsersController(
    IMediator mediator,
    ILogger<AdminUsersController> logger) : AppControllerBase(logger)
{
    /// <summary>List users, optionally filtered to a role (e.g. <c>agriculture</c>).</summary>
    /// <param name="role">Optional role filter. Omit for all users.</param>
    /// <param name="cancellationToken"></param>
    /// <response code="200">Users, ordered by email.</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="403">Caller lacks the admin role.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetAll(
        [FromQuery] string? role,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
            Ok(await mediator.Send(new GetUsersQuery(role), cancellationToken)));

    /// <summary>Create a new user already assigned the agriculture role.</summary>
    /// <response code="201">User created; body is the new user.</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="403">Caller lacks the admin role.</response>
    /// <response code="422">Email taken or password too weak.</response>
    [HttpPost]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> CreateAgricultureUser(
        [FromBody] CreateAgricultureUserCommand command,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
        {
            var user = await mediator.Send(command, cancellationToken);
            return Created(string.Empty, user);
        });

    /// <summary>Grant or revoke the agriculture role for an existing user.</summary>
    /// <param name="id">User id.</param>
    /// <param name="body">Whether the agriculture role should be assigned.</param>
    /// <param name="cancellationToken"></param>
    /// <response code="204">Role updated (idempotent).</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="403">Caller lacks the admin role.</response>
    /// <response code="404">No user with that id.</response>
    [HttpPut("{id:guid}/agriculture")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> SetAgricultureRole(
        Guid id,
        [FromBody] SetAgricultureRoleBody body,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
        {
            await mediator.Send(new SetAgricultureRoleCommand(id, body.Assigned), cancellationToken);
            return NoContent();
        });
}

/// <summary>Body for <c>PUT /api/admin/users/{id}/agriculture</c>.</summary>
public sealed record SetAgricultureRoleBody(bool Assigned);
