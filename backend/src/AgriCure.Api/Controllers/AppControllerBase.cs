using System.Runtime.CompilerServices;
using AgriCure.Application.Common.Auth;
using FluentValidation;
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
        catch (ValidationException ex)
        {
            Logger.LogInformation(
                "Validation failed in {Controller}.{Action}: {ErrorCount} error(s)",
                controllerName, actionName, ex.Errors.Count());

            var errors = ex.Errors
                .GroupBy(e => string.IsNullOrEmpty(e.PropertyName) ? "_" : e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            return ValidationProblem(new ValidationProblemDetails(errors));
        }
        catch (AuthenticationFailedException ex)
        {
            Logger.LogInformation(
                "Authentication failed in {Controller}.{Action}: {Message}",
                controllerName, actionName, ex.Message);

            return Problem(
                title: "Authentication failed.",
                detail: ex.Message,
                statusCode: StatusCodes.Status401Unauthorized);
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
