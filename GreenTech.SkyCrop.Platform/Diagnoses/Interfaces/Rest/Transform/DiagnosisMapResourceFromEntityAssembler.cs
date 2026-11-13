using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.ValueObjects;
using GreenTech.SkyCrop.Platform.Diagnoses.Interfaces.Rest.Resources;

namespace GreenTech.SkyCrop.Platform.Diagnoses.Interfaces.Rest.Transform;

public static class DiagnosisMapResourceFromEntityAssembler
{
    public static DiagnosisMapResource ToResourceFromEntity(
        Diagnosis diagnosis)
    {
        ArgumentNullException.ThrowIfNull(diagnosis);

        var resource =
            DiagnosisResourceFromEntityAssembler.ToResourceFromEntity(
                diagnosis);

        var locatedAnomalies = resource.Anomalies
            .Where(anomaly =>
                anomaly.Latitude.HasValue &&
                anomaly.Longitude.HasValue)
            .ToArray();

        var unlocatedAnomalies =
            resource.Anomalies.Count - locatedAnomalies.Length;

        var completed =
            diagnosis.Status == EDiagnosisStatus.COMPLETED;

        var points = completed
            ? locatedAnomalies
            : Array.Empty<AnomalyResource>();

        var status = points.Length == 0
            ? "UNAVAILABLE"
            : unlocatedAnomalies > 0
                ? "PARTIAL"
                : "AVAILABLE";

        string? unavailableReason = !completed
            ? "DIAGNOSIS_NOT_COMPLETED"
            : resource.Anomalies.Count == 0
                ? "NO_ANOMALIES"
                : points.Length == 0
                    ? "MISSING_COORDINATES"
                    : null;

        return new DiagnosisMapResource(
            diagnosis.Id,
            diagnosis.PlotId,
            diagnosis.Source,
            status,
            unavailableReason,
            resource.Anomalies.Count,
            unlocatedAnomalies,
            points,
            [
                new("LOW", "#2e7d32"),
                new("MEDIUM", "#ac7200"),
                new("HIGH", "#d84315"),
                new("CRITICAL", "#b71c1c")
            ]);
    }
}