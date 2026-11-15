using GreenTech.SkyCrop.Platform.Drones.Domain.Model.Commands;
using GreenTech.SkyCrop.Platform.Drones.Domain.Model.Entities;
using GreenTech.SkyCrop.Platform.Drones.Interfaces.Rest.Resources;

namespace GreenTech.SkyCrop.Platform.Drones.Interfaces.Rest.Transform;

public static class DroneCommandFromResourceAssembler
{
    public static CreateDroneCommand ToCommandFromResource(CreateDroneResource resource) => new(
        resource.Name, resource.Model, resource.SerialNumber, resource.Camera, resource.AutonomyMinutes,
        resource.Battery, resource.ImageUrl, resource.Status);
    public static UpdateDroneCommand ToCommandFromResource(int id, UpdateDroneResource resource) => new(
        id, resource.Name, resource.Model, resource.SerialNumber, resource.Camera, resource.AutonomyMinutes,
        resource.Battery, resource.ImageUrl, resource.Status);
    public static CreateFlightRouteCommand ToCommandFromResource(CreateFlightRouteResource resource) => new(
        resource.Name, resource.PlotId, resource.AltitudeMeters, resource.SpeedMetersPerSecond,
        ToWaypoints(resource.Waypoints));
    public static UpdateFlightRouteCommand ToCommandFromResource(int id, UpdateFlightRouteResource resource) => new(
        id, resource.Name, resource.PlotId, resource.AltitudeMeters, resource.SpeedMetersPerSecond,
        resource.Waypoints is null ? null : ToWaypoints(resource.Waypoints));
    public static PlanFlightCommand ToCommandFromResource(PlanFlightResource resource) => new(
        resource.DroneId, resource.FlightRouteId, resource.PlotId, resource.Status);
    public static CancelFlightCommand ToCommandFromResource(int id, CancelFlightResource resource) => new(id, resource.Status);

    private static RouteWaypoint[] ToWaypoints(IReadOnlyList<WaypointResource>? resources)
    {
        if (resources is null) throw new ArgumentException("Waypoints are required.");
        return resources.Select((point, index) =>
        {
            if (point?.Latitude is null || point.Longitude is null)
                throw new ArgumentException("Every waypoint must contain latitude and longitude.");
            return new RouteWaypoint(index, point.Latitude.Value, point.Longitude.Value);
        }).ToArray();
    }
}
