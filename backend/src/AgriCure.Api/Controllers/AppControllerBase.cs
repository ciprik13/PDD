using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc;

namespace AgriCure.Api.Controllers;

[ApiController]
public abstract class AppControllerBase : ControllerBase
{
    protected ILogger Logger { get; }

    protected AppControllerBase(ILogger logger)
    {
        Logger = logger;
    }

    protected async Task<IActionResult> ExecuteAsync(
        Func<Task<IActionResult>> action,
        [CallerMemberName] string actionName = "")
    {
        var controllerName = GetType().Name;

        using var scope = Logger.BeginScope(
            "{Controller}.{Action}", controllerName, actionName);

        Logger.LogInformation(
            "Handling {Controller}.{Action}", controllerName, actionName);

        try
        {
            return await action();
        }
        catch (Exception ex)
        {
            Logger.LogError(
                ex,
                "Unhandled error in {Controller}.{Action}",
                controllerName,
                actionName);

            return Problem(
                title: "Internal server error.",
                detail: "An unexpected error occurred while processing your request.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
