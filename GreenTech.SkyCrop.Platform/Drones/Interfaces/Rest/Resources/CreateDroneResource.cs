using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GreenTech.SkyCrop.Platform.Drones.Interfaces.Rest.Resources;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateDroneResource(
    [Required, StringLength(120)] string Name,
    [Required, StringLength(120)] string Model,
    [Required, StringLength(120)] string SerialNumber,
    [Required, StringLength(120)] string Camera,
    decimal AutonomyMinutes,
    decimal Battery = 0,
    string? ImageUrl = null,
    string Status = "DISCONNECTED",
    DateTimeOffset? CreatedAt = null);
