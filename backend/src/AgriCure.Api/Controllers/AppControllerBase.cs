using System.Globalization;
using System.Runtime.CompilerServices;
using AgriCure.Application.Common.Auth;
using AgriCure.Application.Common.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

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

            var modelState = new ModelStateDictionary();
            foreach (var failure in ex.Errors)
            {
                modelState.AddModelError(
                    ToCamelCase(failure.PropertyName),
                    failure.ErrorMessage);
            }
            return ValidationProblem(
                modelStateDictionary: modelState,
                statusCode: StatusCodes.Status422UnprocessableEntity);
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
        catch (NotFoundException ex)
        {
            Logger.LogInformation(
                "Resource not found in {Controller}.{Action}: {Message}",
                controllerName, actionName, ex.Message);

            return Problem(
                title: "Not found.",
                detail: ex.Message,
                statusCode: StatusCodes.Status404NotFound);
        }
        catch (ConflictException ex)
        {
            Logger.LogInformation(
                "Conflict in {Controller}.{Action}: {Message}",
                controllerName, actionName, ex.Message);

            return Problem(
                title: "Conflict.",
                detail: ex.Message,
                statusCode: StatusCodes.Status409Conflict);
        }
        catch (UnprocessableEntityException ex)
        {
            Logger.LogInformation(
                "Unprocessable entity in {Controller}.{Action}: {ErrorCount} error(s)",
                controllerName, actionName, ex.Errors.Count);

            var modelState = new ModelStateDictionary();
            foreach (var (property, messages) in ex.Errors)
            {
                foreach (var message in messages)
                {
                    modelState.AddModelError(ToCamelCase(property), message);
                }
            }
            return ValidationProblem(
                modelStateDictionary: modelState,
                statusCode: StatusCodes.Status422UnprocessableEntity);
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

    /// <summary>Lowercase the first character so the JSON `errors` map keys match the rest of the camelCase JSON.</summary>
    private static string ToCamelCase(string property)
    {
        if (string.IsNullOrEmpty(property))
        {
            return string.Empty;
        }
        if (char.IsLower(property[0]))
        {
            return property;
        }
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{char.ToLowerInvariant(property[0])}{property[1..]}");
    }
}
