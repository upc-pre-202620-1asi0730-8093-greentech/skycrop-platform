namespace GreenTech.SkyCrop.Platform.Drones.Interfaces.Rest.Resources;

public sealed record FlightRouteResource(
    int Id, string Name, int PlotId, decimal AltitudeMeters,
    decimal SpeedMetersPerSecond, IReadOnlyList<WaypointResource> Waypoints);
