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
}
