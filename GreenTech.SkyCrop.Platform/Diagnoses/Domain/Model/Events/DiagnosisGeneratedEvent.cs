using GreenTech.SkyCrop.Platform.Shared.Domain.Model.Events;

namespace GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Events;

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
