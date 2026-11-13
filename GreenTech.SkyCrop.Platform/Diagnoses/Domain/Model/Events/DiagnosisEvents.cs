using GreenTech.SkyCrop.Platform.Shared.Domain.Model.Events;

namespace GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Events;

public sealed record DiagnosisRequestedEvent(
    int DiagnosisId,
    int PlotId,
    int FlightId,
    DateTimeOffset RequestedAt) : IEvent;

public sealed record DiagnosisProcessingStartedEvent(
    int DiagnosisId,
    int PlotId,
    int FlightId) : IEvent;

public sealed record DiagnosisGeneratedEvent(
    int DiagnosisId,
    int PlotId,
    int FlightId,
    DateTimeOffset MonitoredAt,
    DateTimeOffset GeneratedAt,
    string Scenario,
    string Source,
    double AverageNdvi,
    string Observations,
    int AnomalyCount) : IEvent;

public sealed record AgronomicAnomalyDetectedEvent(
    int DiagnosisId,
    int PlotId,
    int AnomalyId,
    string Type,
    string Severity,
    string Source,
    DateTimeOffset MonitoredAt) : IEvent;

public sealed record DiagnosisFailedEvent(
    int DiagnosisId,
    int PlotId,
    int FlightId,
    string Reason) : IEvent;