using GreenTech.SkyCrop.Platform.Shared.Domain.Model.Errors;

namespace GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Errors;

public static class DiagnosisErrors
{
    public static Error InvalidRequest(string message) =>
        Error.Validation("Diagnoses.InvalidRequest", message);

    public static readonly Error InvalidPlotId = Error.Validation(
        "Diagnoses.InvalidPlotId",
        "A positive plot identifier is required.");

    public static readonly Error InvalidDiagnosisId = Error.Validation(
        "Diagnoses.InvalidDiagnosisId",
        "A positive diagnosis identifier is required.");

    public static readonly Error NotFound = Error.NotFound(
        "Diagnoses.DiagnosisNotFound",
        "The diagnosis was not found.");
}