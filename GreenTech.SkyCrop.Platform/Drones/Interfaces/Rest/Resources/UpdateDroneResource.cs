using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GreenTech.SkyCrop.Platform.Drones.Interfaces.Rest.Resources;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record UpdateDroneResource(
    string? Name = null, string? Model = null, string? SerialNumber = null,
    string? Camera = null, decimal? AutonomyMinutes = null, decimal? Battery = null,
    string? ImageUrl = null, string? Status = null);
