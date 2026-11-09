using GreenTech.SkyCrop.Platform.Drones.Domain.Model.Errors;
using GreenTech.SkyCrop.Platform.Shared.Application.Model;
using GreenTech.SkyCrop.Platform.Shared.Domain.Model.Errors;
using Microsoft.AspNetCore.Mvc;

namespace GreenTech.SkyCrop.Platform.Drones.Interfaces.Rest.Transform;

public static class DronesActionResultAssembler
{
    public static async Task<IActionResult> Handle<T>(ControllerBase controller,
        Func<Task<Result<T>>> operation, Func<T, IActionResult> onSuccess)
    {
        try
        {
            var result = await operation();
            return result.Match(onSuccess, error => ToProblem(controller, error));
        }
        catch (ArgumentException exception)
        {
            return ToProblem(controller, DroneErrors.InvalidData(exception.Message));
        }
    }

    public static IActionResult ToProblem(ControllerBase controller, Error error) => controller.Problem(
        statusCode: error.Type switch
        {
            EErrorType.NotFound => StatusCodes.Status404NotFound,
            EErrorType.Conflict => StatusCodes.Status409Conflict,
            EErrorType.Validation => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        }, title: error.Code, detail: error.Message);
}
