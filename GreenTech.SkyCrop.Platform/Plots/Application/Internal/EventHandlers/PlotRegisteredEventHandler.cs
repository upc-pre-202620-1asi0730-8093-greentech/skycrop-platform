using Cortex.Mediator;
using GreenTech.SkyCrop.Platform.Plots.Domain.Model.Events;
using GreenTech.SkyCrop.Platform.Plots.Interfaces.Events;
using GreenTech.SkyCrop.Platform.Shared.Application.Internal.EventHandlers;

namespace GreenTech.SkyCrop.Platform.Plots.Application.Internal.EventHandlers;

public sealed class PlotRegisteredEventHandler(IMediator mediator) : IEventHandler<PlotRegisteredEvent>
{
    public Task Handle(PlotRegisteredEvent domainEvent, CancellationToken cancellationToken) =>
        mediator.PublishAsync(new PlotRegisteredIntegrationEvent(domainEvent.PlotId), cancellationToken);
}
