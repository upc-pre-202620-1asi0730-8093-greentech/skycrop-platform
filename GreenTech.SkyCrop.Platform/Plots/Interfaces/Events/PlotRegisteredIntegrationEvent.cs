using Cortex.Mediator.Notifications;

namespace GreenTech.SkyCrop.Platform.Plots.Interfaces.Events;

/// <summary>Published when a parcel has been registered.</summary>
public sealed record PlotRegisteredIntegrationEvent(int PlotId) : INotification;
