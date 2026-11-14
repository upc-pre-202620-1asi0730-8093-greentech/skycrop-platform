using Cortex.Mediator.Notifications;

namespace GreenTech.SkyCrop.Platform.Diagnoses.Interfaces.Events;

public sealed record DiagnosisFailedIntegrationEvent(
    int DiagnosisId,
    int PlotId,
    int FlightId,
    string Reason) : INotification;
