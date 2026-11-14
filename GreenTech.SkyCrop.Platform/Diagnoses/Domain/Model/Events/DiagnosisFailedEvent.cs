using GreenTech.SkyCrop.Platform.Shared.Domain.Model.Events;

namespace GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Events;

public sealed record DiagnosisFailedEvent(
    int DiagnosisId,
    int PlotId,
    int FlightId,
    string Reason) : IEvent;
