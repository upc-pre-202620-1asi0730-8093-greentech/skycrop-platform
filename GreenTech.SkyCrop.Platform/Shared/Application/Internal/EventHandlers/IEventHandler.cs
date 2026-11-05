using GreenTech.SkyCrop.Platform.Shared.Domain.Model.Events;
using Cortex.Mediator.Notifications;

namespace GreenTech.SkyCrop.Platform.Shared.Application.Internal.EventHandlers;

public interface IEventHandler<in TEvent> : INotificationHandler<TEvent> where TEvent : IEvent
{
}