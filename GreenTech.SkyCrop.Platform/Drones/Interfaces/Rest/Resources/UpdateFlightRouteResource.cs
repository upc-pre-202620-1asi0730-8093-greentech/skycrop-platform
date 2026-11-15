using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GreenTech.SkyCrop.Platform.Drones.Interfaces.Rest.Resources;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record UpdateFlightRouteResource(
    string? Name = null, int? PlotId = null, decimal? AltitudeMeters = null,
    decimal? SpeedMetersPerSecond = null, IReadOnlyList<WaypointResource>? Waypoints = null);
