using GreenTech.SkyCrop.Platform.Shared.Domain.Model.Events;

namespace GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Events;

public sealed record DiagnosisProcessingStartedEvent(
    int DiagnosisId,
    int PlotId,
    int FlightId) : IEvent;
