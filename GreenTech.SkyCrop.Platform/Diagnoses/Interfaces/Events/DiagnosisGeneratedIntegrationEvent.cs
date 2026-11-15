using Cortex.Mediator.Notifications;

namespace GreenTech.SkyCrop.Platform.Diagnoses.Interfaces.Events;

/// <summary>
/// Published after EF saves a completed diagnosis.
/// Consumers must preserve the simulated source and monitoring date.
/// </summary>
public sealed record DiagnosisGeneratedIntegrationEvent(
    int DiagnosisId,
    int PlotId,
    int FlightId,
    DateTimeOffset MonitoredAt,
    DateTimeOffset GeneratedAt,
    string Scenario,
    string Source,
    double AverageNdvi,
    string Observations,
    int AnomalyCount) : INotification;
