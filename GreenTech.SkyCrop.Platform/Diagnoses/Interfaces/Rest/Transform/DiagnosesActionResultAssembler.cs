using GreenTech.SkyCrop.Platform.Shared.Domain.Model.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GreenTech.SkyCrop.Platform.Diagnoses.Interfaces.Rest.Transform;

public static class DiagnosesActionResultAssembler
{
    public static ObjectResult ToProblem(Error error)
    {
        var status = error.Code == "Diagnoses.AccessNotConfigured"
            ? StatusCodes.Status503ServiceUnavailable
            : error.Type switch
            {
                EErrorType.Validation => StatusCodes.Status400BadRequest,
                EErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
                EErrorType.Forbidden => StatusCodes.Status403Forbidden,
                EErrorType.NotFound => StatusCodes.Status404NotFound,
                EErrorType.Conflict => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status500InternalServerError
            };

        var problem = new ProblemDetails
        {
            Status = status,
            Title = error.Code,
            Detail = error.Message
        };

        var response = new ObjectResult(problem)
        {
            StatusCode = status
        };

        response.ContentTypes.Add("application/problem+json");

        return response;
    }
}