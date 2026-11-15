using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GreenTech.SkyCrop.Platform.Drones.Interfaces.Rest.Resources;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record WaypointResource(
    [Required, Range(-90, 90)] double? Latitude,
    [Required, Range(-180, 180)] double? Longitude);
