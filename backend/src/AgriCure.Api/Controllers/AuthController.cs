using AgriCure.Application.Common.DTOs;
using AgriCure.Application.Features.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriCure.Api.Controllers;

[Route("api/auth")]
public sealed class AuthController(
    IMediator mediator,
    ILogger<AuthController> logger) : AppControllerBase(logger)
{
    /// <summary>Register a new user account and return an access + refresh token pair.</summary>
    /// <response code="200">Registration succeeded; returns the token pair.</response>
    /// <response code="422">Validation failed — email or password did not meet requirements.</response>
    /// <response code="500">Unhandled error — see ProblemDetails.</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Register(
        [FromBody] RegisterCommand command,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async () => Ok(await mediator.Send(command, cancellationToken)));

    /// <summary>Exchange an existing email + password for a token pair.</summary>
    /// <response code="200">Login succeeded.</response>
    /// <response code="401">Email + password did not match an active account.</response>
    /// <response code="422">Validation failed.</response>
    /// <response code="500">Unhandled error.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Login(
        [FromBody] LoginCommand command,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async () => Ok(await mediator.Send(command, cancellationToken)));

    /// <summary>Rotate a refresh token, returning a fresh access + refresh pair.</summary>
    /// <response code="200">Rotation succeeded; the previous refresh token is now revoked.</response>
    /// <response code="401">Refresh token unknown, expired, or already revoked.</response>
    /// <response code="422">Validation failed.</response>
    /// <response code="500">Unhandled error.</response>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Refresh(
        [FromBody] RefreshTokenCommand command,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async () => Ok(await mediator.Send(command, cancellationToken)));

    /// <summary>Revoke a refresh token. Idempotent — already-revoked tokens still return 204.</summary>
    /// <response code="204">Logout succeeded (or token was already revoked).</response>
    /// <response code="422">Validation failed.</response>
    /// <response code="500">Unhandled error.</response>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Logout(
        [FromBody] LogoutCommand command,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
        {
            await mediator.Send(command, cancellationToken);
            return NoContent();
        });
}
