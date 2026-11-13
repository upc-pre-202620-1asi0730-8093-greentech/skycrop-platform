using Cortex.Mediator;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Events;
using GreenTech.SkyCrop.Platform.Diagnoses.Interfaces.Events;
using GreenTech.SkyCrop.Platform.Shared.Application.Internal.EventHandlers;

namespace GreenTech.SkyCrop.Platform.Diagnoses.Application.Internal.EventHandlers;

public sealed class DiagnosisGeneratedEventHandler(IMediator mediator)
    : IEventHandler<DiagnosisGeneratedEvent>
{
    public Task Handle(
        DiagnosisGeneratedEvent domainEvent,
        CancellationToken cancellationToken)
    {
        return mediator.PublishAsync(
            new DiagnosisGeneratedIntegrationEvent(
                domainEvent.DiagnosisId,
                domainEvent.PlotId,
                domainEvent.FlightId,
                domainEvent.MonitoredAt,
                domainEvent.GeneratedAt,
                domainEvent.Scenario,
                domainEvent.Source,
                domainEvent.AverageNdvi,
                domainEvent.Observations,
                domainEvent.AnomalyCount),
            cancellationToken);
    }
}

public sealed class AgronomicAnomalyDetectedEventHandler(IMediator mediator)
    : IEventHandler<AgronomicAnomalyDetectedEvent>
{
    public Task Handle(
        AgronomicAnomalyDetectedEvent domainEvent,
        CancellationToken cancellationToken)
    {
        return mediator.PublishAsync(
            new AgronomicAnomalyDetectedIntegrationEvent(
                domainEvent.DiagnosisId,
                domainEvent.PlotId,
                domainEvent.AnomalyId,
                domainEvent.Type,
                domainEvent.Severity,
                domainEvent.Source,
                domainEvent.MonitoredAt),
            cancellationToken);
    }
}

public sealed class DiagnosisFailedEventHandler(IMediator mediator)
    : IEventHandler<DiagnosisFailedEvent>
{
    public Task Handle(
        DiagnosisFailedEvent domainEvent,
        CancellationToken cancellationToken)
    {
        return mediator.PublishAsync(
            new DiagnosisFailedIntegrationEvent(
                domainEvent.DiagnosisId,
                domainEvent.PlotId,
                domainEvent.FlightId,
                domainEvent.Reason),
            cancellationToken);
    }
}