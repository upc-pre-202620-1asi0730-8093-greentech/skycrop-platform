using GreenTech.SkyCrop.Platform.Reports.Domain.Model;
using GreenTech.SkyCrop.Platform.Reports.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Resources.Errors;
using GreenTech.SkyCrop.Platform.Shared.Application.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using ProblemDetailsFactory = GreenTech.SkyCrop.Platform.Shared.Interfaces.Rest.ProblemDetails.ProblemDetailsFactory;

namespace GreenTech.SkyCrop.Platform.Reports.Interfaces.Rest.Transform;

public static class ReportsActionResultAssembler
{
    private static int ToStatusCodeFromReportsError(ReportsError error)
    {
        return error switch
        {
            ReportsError.ReportNotFound => StatusCodes.Status404NotFound,
            ReportsError.ReportAlreadyRegistered => StatusCodes.Status409Conflict,
            ReportsError.InvalidReportData => StatusCodes.Status400BadRequest,
            ReportsError.OperationCancelled => StatusCodes.Status409Conflict,
            ReportsError.DatabaseError => StatusCodes.Status500InternalServerError,
            ReportsError.InternalServerError => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status400BadRequest
        };
    }


    public static IActionResult ToActionResultFromCreateReportResult(
        ControllerBase controller,
        Result<Report> result,
        IStringLocalizer<ErrorMessages> errorLocalizer,
        ProblemDetailsFactory problemDetailsFactory,
        Func<Report, IActionResult> successAction)
    {
        if (result.IsSuccess) return successAction(result.Value!);

        var statusCode = ToStatusCodeFromReportsError((ReportsError)result.Error!);
        return problemDetailsFactory.CreateProblemDetails(controller, statusCode, result.Error, result.Message);
    }

    public static IActionResult ToActionResultFromGetReportByIdResult(
        ControllerBase controller,
        Report? report,
        IStringLocalizer<ErrorMessages> errorLocalizer,
        ProblemDetailsFactory problemDetailsFactory,
        Func<Report, IActionResult> successAction
    )
    {
        if (report is null)
            return problemDetailsFactory.CreateProblemDetails(
                controller,
                ToStatusCodeFromReportsError(ReportsError.ReportNotFound),
                ReportsError.ReportNotFound,
                errorLocalizer[nameof(ReportsError.ReportNotFound)]
            );
        return successAction(report);
    }
}