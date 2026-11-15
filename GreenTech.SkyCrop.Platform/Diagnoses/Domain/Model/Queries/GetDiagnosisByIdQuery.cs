namespace GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Queries;

/// <summary>
/// Requests a diagnosis by its identifier.
/// </summary>
public sealed record GetDiagnosisByIdQuery(int DiagnosisId);