using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Diagnoses.Interfaces.Rest.Resources;

namespace GreenTech.SkyCrop.Platform.Diagnoses.Interfaces.Rest.Transform;

public static class DiagnosisResourceFromEntityAssembler
{
    public static DiagnosisResource ToResourceFromEntity(Diagnosis diagnosis)
    {
        return new DiagnosisResource(
            diagnosis.Id,
            diagnosis.PlotId,
            diagnosis.FlightId,
            diagnosis.ImageIds.ToArray(),
            diagnosis.Status.ToString(),
            diagnosis.Scenario,
            diagnosis.Source,
            diagnosis.MonitoredAt,
            diagnosis.RequestedAt,
            diagnosis.GeneratedAt,
            diagnosis.Observations,
            diagnosis.AverageNdvi?.Value,
            diagnosis.FailureReason,
            diagnosis.Anomalies.Select(anomaly => new AnomalyResource(
                anomaly.Id,
                anomaly.Type,
                anomaly.Severity,
                anomaly.Description,
                anomaly.Location,
                anomaly.Latitude,
                anomaly.Longitude)).ToArray());
    }
}