using GreenTech.SkyCrop.Platform.Drones.Domain.Model.Entities;

namespace GreenTech.SkyCrop.Platform.Drones.Domain.Model.Commands;

public sealed record CreateFlightRouteCommand(string Name, int PlotId, decimal AltitudeMeters, decimal SpeedMetersPerSecond, IReadOnlyList<RouteWaypoint> Waypoints);
