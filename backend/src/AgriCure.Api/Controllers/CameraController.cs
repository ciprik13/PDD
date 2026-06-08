using AgriCure.Application.Features.Telemetry;
using AgriCure.Application.Features.Telemetry.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace AgriCure.Api.Controllers;

/// <summary>
/// Live camera surface. Frame metadata is read from stored detections; the live video
/// feed and GPS are proxied from the on-prem Jetson device. The Jetson is reached over
/// a private mesh network (e.g. Tailscale) at the URLs configured in <c>Jetson:StreamUrl</c>
/// and <c>Jetson:BaseUrl</c> — no device address is ever exposed to the browser.
/// </summary>
[Route("api/camera")]
public sealed class CameraController(
    IMediator mediator,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    IMemoryCache cache,
    ILogger<CameraController> logger) : AppControllerBase(logger)
{
    private const string TokenPrefix = "stream_token:";
    private const string JetsonClient = "jetson";

    /// <summary>Metadata for the most recent captured frame, derived from the latest detection.</summary>
    /// <response code="200">Frame metadata (fields null when there's no data yet).</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="403">Caller lacks the required role.</response>
    [Authorize(Roles = "admin,agriculture")]
    [HttpGet("frame")]
    [ProducesResponseType(typeof(CameraFrameDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetFrame(CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
            Ok(await mediator.Send(new GetCameraFrameQuery(), cancellationToken)));

    /// <summary>
    /// Issue a short-lived (60s), single-use token for the MJPEG stream. The stream itself
    /// is consumed by an <c>&lt;img&gt;</c> tag, which can't send an Authorization header —
    /// hence the token-in-query-string handshake.
    /// </summary>
    /// <response code="200">Token issued.</response>
    /// <response code="401">Caller is not authenticated.</response>
    [Authorize(Roles = "admin,agriculture")]
    [HttpGet("token")]
    [ProducesResponseType(typeof(StreamTokenDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public IActionResult GetStreamToken()
    {
        var token = Guid.NewGuid().ToString("N");
        cache.Set($"{TokenPrefix}{token}", true, TimeSpan.FromSeconds(60));
        Logger.LogInformation("Issued camera stream token for {User}", User.Identity?.Name);
        return Ok(new StreamTokenDto(token, 60));
    }

    /// <summary>
    /// Proxy the MJPEG stream from the Jetson. Authenticated by the single-use token from
    /// <c>GET /api/camera/token</c>. Returns 502 when the device is unreachable.
    /// </summary>
    /// <param name="token">Token from <c>GET /api/camera/token</c>.</param>
    /// <param name="cancellationToken"></param>
    [HttpGet("stream")]
    public async Task Stream([FromQuery] string? token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token) ||
            !cache.TryGetValue($"{TokenPrefix}{token}", out _))
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            Response.ContentType = "application/problem+json";
            await Response.WriteAsJsonAsync(new
            {
                type = "https://tools.ietf.org/html/rfc9110#section-15.5.2",
                title = "Unauthorized.",
                status = 401,
                detail = "Stream token missing, invalid or expired. Request a new one from /api/camera/token.",
            }, cancellationToken);
            return;
        }

        // One token = one connection.
        cache.Remove($"{TokenPrefix}{token}");

        var jetsonUrl = configuration["Jetson:StreamUrl"];
        if (string.IsNullOrWhiteSpace(jetsonUrl))
        {
            await WriteCameraOfflineAsync("Jetson:StreamUrl is not configured.", cancellationToken);
            return;
        }

        var client = httpClientFactory.CreateClient(JetsonClient);

        try
        {
            var upstream = await client.GetAsync(
                jetsonUrl,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            Response.StatusCode = (int)upstream.StatusCode;
            Response.ContentType = upstream.Content.Headers.ContentType?.ToString()
                ?? "multipart/x-mixed-replace; boundary=frame";

            HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();

            Logger.LogInformation("Proxying MJPEG stream from {Url}", jetsonUrl);

            await using var stream = await upstream.Content.ReadAsStreamAsync(cancellationToken);
            await stream.CopyToAsync(Response.Body, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            Logger.LogInformation("Camera stream closed by client.");
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "Could not reach the Jetson stream at {Url}", jetsonUrl);
            if (!Response.HasStarted)
            {
                await WriteCameraOfflineAsync("The Jetson is not responding.", cancellationToken);
            }
        }
    }

    /// <summary>Proxy live GPS from the Jetson.</summary>
    /// <response code="200">GPS payload from the device (passed through verbatim).</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="502">Jetson offline or not configured.</response>
    [Authorize(Roles = "admin,agriculture")]
    [HttpGet("gps")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> GetGps(CancellationToken cancellationToken)
    {
        var jetsonBase = configuration["Jetson:BaseUrl"];
        if (string.IsNullOrWhiteSpace(jetsonBase))
        {
            return StatusCode(StatusCodes.Status502BadGateway);
        }

        var client = httpClientFactory.CreateClient(JetsonClient);
        try
        {
            var response = await client.GetAsync($"{jetsonBase.TrimEnd('/')}/gps", cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return Content(json, "application/json");
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "Could not reach the Jetson GPS endpoint.");
            return StatusCode(StatusCodes.Status502BadGateway);
        }
    }

    private async Task WriteCameraOfflineAsync(string detail, CancellationToken cancellationToken)
    {
        Response.StatusCode = StatusCodes.Status502BadGateway;
        Response.ContentType = "application/problem+json";
        await Response.WriteAsJsonAsync(new
        {
            type = "https://tools.ietf.org/html/rfc9110#section-15.6.3",
            title = "Camera offline.",
            status = 502,
            detail,
        }, cancellationToken);
    }
}

/// <summary>Short-lived token for the MJPEG camera stream.</summary>
public sealed record StreamTokenDto(string Token, int ExpiresInSeconds);
