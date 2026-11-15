namespace GreenTech.SkyCrop.Platform.Diagnoses.Interfaces.Acl;

/// <summary>
/// Completed diagnosis information exposed to other bounded contexts.
/// </summary>
public sealed record DiagnosisSummary(
    int DiagnosisId,
    int PlotId,
    DateTimeOffset MonitoredAt,
    DateTimeOffset GeneratedAt,
    string Scenario,
    string Source,
    double AverageNdvi,
    string Observations,
    int AnomalyCount);