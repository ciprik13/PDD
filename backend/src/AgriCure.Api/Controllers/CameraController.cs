using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace AgriCure.Api.Controllers;

[Route("api/camera")]
public sealed class CameraController(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    IMemoryCache cache,
    ILogger<CameraController> logger) : AppControllerBase(logger)
{
    private const string TokenPrefix = "stream_token:";

    /// <summary>
    /// Genereaza un token de scurta durata (60s) pentru accesul la stream.
    /// Necesita autentificare JWT standard.
    /// </summary>
    /// <response code="200">Token generat.</response>
    /// <response code="401">Utilizator neautentificat.</response>
    [Authorize]
    [HttpGet("token")]
    [ProducesResponseType(typeof(StreamTokenDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public IActionResult GetStreamToken()
    {
        var token = Guid.NewGuid().ToString("N");

        cache.Set(
            $"{TokenPrefix}{token}",
            true,
            TimeSpan.FromSeconds(60));

        logger.LogInformation("Stream token generat pentru utilizatorul {User}", User.Identity?.Name);

        return Ok(new StreamTokenDto(token, 60));
    }

    /// <summary>
    /// Proxy MJPEG stream de la Jetson. Autentificat cu stream token din query string.
    /// </summary>
    /// <param name="token">Token obtinut de la GET /api/camera/token</param>
    /// <response code="200">MJPEG stream continuu.</response>
    /// <response code="401">Token lipsa, invalid sau expirat.</response>
    /// <response code="502">Jetson offline sau inaccesibil.</response>
    [HttpGet("stream")]
    public async Task Stream([FromQuery] string? token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token) ||
            !cache.TryGetValue($"{TokenPrefix}{token}", out _))
        {
            Response.StatusCode  = StatusCodes.Status401Unauthorized;
            Response.ContentType = "application/problem+json";
            await Response.WriteAsJsonAsync(new
            {
                type   = "https://tools.ietf.org/html/rfc9110#section-15.5.2",
                title  = "Unauthorized.",
                status = 401,
                detail = "Stream token lipsa, invalid sau expirat. Obtine un token nou de la /api/camera/token.",
            }, cancellationToken);
            return;
        }

        // Consuma token-ul — un token = o conexiune
        cache.Remove($"{TokenPrefix}{token}");

        var jetsonUrl = configuration["Jetson:StreamUrl"]
            ?? throw new InvalidOperationException("Jetson:StreamUrl nu este configurat.");

        var client = httpClientFactory.CreateClient("jetson");

        try
        {
            var upstream = await client.GetAsync(
                jetsonUrl,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            Response.StatusCode  = (int)upstream.StatusCode;
            Response.ContentType = upstream.Content.Headers.ContentType?.ToString()
                ?? "multipart/x-mixed-replace; boundary=frame";

            var bufferingFeature = HttpContext.Features
                .Get<Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature>();
            bufferingFeature?.DisableBuffering();

            logger.LogInformation("Stream MJPEG pornit de la {Url}", jetsonUrl);

            await using var stream = await upstream.Content.ReadAsStreamAsync(cancellationToken);
            await stream.CopyToAsync(Response.Body, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Stream inchis de client.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nu pot ajunge la Jetson: {Url}", jetsonUrl);

            if (!Response.HasStarted)
            {
                Response.StatusCode  = StatusCodes.Status502BadGateway;
                Response.ContentType = "application/problem+json";
                await Response.WriteAsJsonAsync(new
                {
                    type   = "https://tools.ietf.org/html/rfc9110#section-15.6.3",
                    title  = "Camera offline.",
                    status = 502,
                    detail = "Jetson-ul nu raspunde. Verifica reteaua sau camera.",
                }, cancellationToken);
            }
        }
    }

/// <summary>Proxy date GPS live de la Jetson.</summary>
[Authorize]
[HttpGet("gps")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
public async Task<IActionResult> GetGps(CancellationToken cancellationToken)
{
    var jetsonBase = configuration["Jetson:BaseUrl"]
        ?? throw new InvalidOperationException("Jetson:BaseUrl nu este configurat.");

    var client = httpClientFactory.CreateClient("jetson");
    try
    {
        var response = await client.GetAsync($"{jetsonBase}/gps", cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return Content(json, "application/json");
    }
    catch (HttpRequestException ex)
    {
        logger.LogError(ex, "Nu pot ajunge la GPS Jetson.");
        return StatusCode(StatusCodes.Status502BadGateway);
    }
}
}
public sealed record StreamTokenDto(string Token, int ExpiresInSeconds);
