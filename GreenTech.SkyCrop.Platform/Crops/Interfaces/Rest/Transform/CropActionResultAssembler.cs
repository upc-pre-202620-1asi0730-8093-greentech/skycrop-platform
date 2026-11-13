using GreenTech.SkyCrop.Platform.Crops.Domain.Model;
using GreenTech.SkyCrop.Platform.Crops.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Crops.Domain.Model.Errors;
using GreenTech.SkyCrop.Platform.Resources.Errors;
using GreenTech.SkyCrop.Platform.Shared.Application.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Localization;
using ProblemDetailsFactory = GreenTech.SkyCrop.Platform.Shared.Interfaces.Rest.ProblemDetails.ProblemDetailsFactory;

namespace GreenTech.SkyCrop.Platform.Crops.Interfaces.Rest.Transform;

public static class CropActionResultAssembler
{
    private static int ToStatusCodeFromCropsError(CropsError error)
    {
        return error switch
        {
            CropsError.CropNotFound => StatusCodes.Status404NotFound,
            CropsError.CropAlreadyRegistered => StatusCodes.Status409Conflict,
            CropsError.InvalidCropData => StatusCodes.Status400BadRequest,
            CropsError.OperationCancelled => StatusCodes.Status409Conflict,
            CropsError.DatabaseError => StatusCodes.Status500InternalServerError,
            CropsError.InternalServerError => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status400BadRequest
        };
    }

    public static IActionResult ToActionResultFromCreateCropResult(
        ControllerBase controller,
        Result<Crop> result,
        IStringLocalizer<ErrorMessages> errorLocalizer,
        ProblemDetailsFactory problemDetailsFactory,
        Func<Crop, IActionResult> successAction
    )
    {
        if (result.IsSuccess) return successAction(result.Value!);

        var statusCode = ToStatusCodeFromCropsError((CropsError)result.Error!);
        return problemDetailsFactory.CreateProblemDetails(controller, statusCode, result.Error,
            errorLocalizer[$"{result.Error}"]);
    }

    public static IActionResult ToActionResultFromGetCropByIdResult(
        ControllerBase controller,
        Crop? crop,
        IStringLocalizer<ErrorMessages> errorLocalizer,
        ProblemDetailsFactory problemDetailsFactory,
        Func<Crop, IActionResult> successAction
    )
    {
        if (crop is null)
            return problemDetailsFactory.CreateProblemDetails(
                controller, ToStatusCodeFromCropsError(CropsError.CropNotFound),
                CropsError.CropNotFound,
                errorLocalizer[nameof(CropsError.CropNotFound)]
                );
        return successAction(crop);
    }
}