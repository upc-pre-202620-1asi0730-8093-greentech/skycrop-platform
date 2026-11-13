namespace GreenTech.SkyCrop.Platform.Diagnoses.Interfaces.Rest.Resources;

public sealed record AnomalyResource(
    int Id,
    string Type,
    string Severity,
    string Description,
    string Location,
    double? Latitude,
    double? Longitude);

public sealed record DiagnosisResource(
    int Id,
    int PlotId,
    int FlightId,
    IReadOnlyCollection<int> ImageIds,
    string Status,
    string Scenario,
    string Source,
    DateTimeOffset MonitoredAt,
    DateTimeOffset RequestedAt,
    DateTimeOffset? GeneratedAt,
    string Observations,
    double? AverageNdvi,
    string? FailureReason,
    IReadOnlyCollection<AnomalyResource> Anomalies);