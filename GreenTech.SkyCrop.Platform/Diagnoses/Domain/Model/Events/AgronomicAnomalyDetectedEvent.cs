using GreenTech.SkyCrop.Platform.Shared.Domain.Model.Events;

namespace GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Events;

public sealed record AgronomicAnomalyDetectedEvent(
    int DiagnosisId,
    int PlotId,
    int AnomalyId,
    string Type,
    string Severity,
    string Source,
    DateTimeOffset MonitoredAt) : IEvent;
