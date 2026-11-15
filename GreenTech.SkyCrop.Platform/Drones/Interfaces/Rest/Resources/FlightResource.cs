namespace GreenTech.SkyCrop.Platform.Drones.Interfaces.Rest.Resources;

public sealed record FlightResource(
    int Id, int DroneId, int FlightRouteId, int PlotId, string Status,
    DateTimeOffset? CreatedAt, DateTimeOffset? StartedAt, decimal? DurationMinutes);
