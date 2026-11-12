using GreenTech.SkyCrop.Platform.Shared.Domain.Model.Events;

namespace GreenTech.SkyCrop.Platform.Plots.Domain.Model.Events;

public sealed record PlotRegisteredEvent(int PlotId) : IEvent;
