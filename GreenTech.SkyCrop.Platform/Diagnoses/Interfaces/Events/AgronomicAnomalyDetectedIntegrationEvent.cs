using Cortex.Mediator.Notifications;

namespace GreenTech.SkyCrop.Platform.Diagnoses.Interfaces.Events;

public sealed record AgronomicAnomalyDetectedIntegrationEvent(
    int DiagnosisId,
    int PlotId,
    int AnomalyId,
    string Type,
    string Severity,
    string Source,
    DateTimeOffset MonitoredAt) : INotification;
