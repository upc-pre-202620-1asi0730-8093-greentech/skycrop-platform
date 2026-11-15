using GreenTech.SkyCrop.Platform.Drones.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Drones.Domain.Model.Entities;
using GreenTech.SkyCrop.Platform.Drones.Interfaces.Rest.Resources;

namespace GreenTech.SkyCrop.Platform.Drones.Interfaces.Rest.Transform;

public static class DroneResourceFromEntityAssembler
{
    public static DroneResource ToResourceFromEntity(Drone drone) => new(drone.Id, drone.Name,
        drone.Model, drone.SerialNumber.Value, drone.Camera, drone.AutonomyMinutes,
        drone.Battery.Value, drone.Status.ToString(), drone.ImageUrl, drone.CreatedAt);
    public static FlightRouteResource ToResourceFromEntity(FlightRoute route) => new(route.Id,
        route.Name, route.PlotId, route.Parameters.AltitudeMeters, route.Parameters.SpeedMetersPerSecond,
        route.Waypoints.OrderBy(p => p.Sequence).Select(p => new WaypointResource(p.Latitude, p.Longitude)).ToArray());
    public static FlightResource ToResourceFromEntity(Flight flight) => new(flight.Id, flight.DroneId,
        flight.FlightRouteId, flight.PlotId, flight.Status.ToString(), flight.CreatedAt,
        flight.StartedAt, flight.DurationMinutes);
    public static AerialImageResource ToResourceFromEntity(AerialImage image) => new(image.Id,
        image.FlightId, image.FileUrl, image.CapturedAt);
}
