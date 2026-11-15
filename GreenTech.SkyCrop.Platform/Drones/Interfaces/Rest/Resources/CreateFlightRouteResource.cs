using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GreenTech.SkyCrop.Platform.Drones.Interfaces.Rest.Resources;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateFlightRouteResource(
    [Required, StringLength(120)] string Name,
    int PlotId, decimal AltitudeMeters, decimal SpeedMetersPerSecond,
    [Required, MinLength(2)] IReadOnlyList<WaypointResource> Waypoints);
