namespace GreenTech.SkyCrop.Platform.Diagnoses.Interfaces.Rest.Resources;

public sealed record DiagnosisMapLegendResource(
    string Severity,
    string Color);

public sealed record DiagnosisMapResource(
    int DiagnosisId,
    int PlotId,
    string Source,
    string Status,
    string? UnavailableReason,
    int TotalAnomalies,
    int UnlocatedAnomalies,
    IReadOnlyCollection<AnomalyResource> Points,
    IReadOnlyCollection<DiagnosisMapLegendResource> Legend);