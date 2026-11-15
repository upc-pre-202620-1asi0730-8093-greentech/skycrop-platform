using GreenTech.SkyCrop.Platform.Drones.Domain.Model.Entities;

namespace GreenTech.SkyCrop.Platform.Drones.Domain.Model.Commands;

public sealed record UpdateFlightRouteCommand(int RouteId, string? Name = null, int? PlotId = null, decimal? AltitudeMeters = null, decimal? SpeedMetersPerSecond = null, IReadOnlyList<RouteWaypoint>? Waypoints = null);
