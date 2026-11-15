namespace GreenTech.SkyCrop.Platform.Drones.Interfaces.Rest.Resources;

public sealed record DroneResource(
    int Id, string Name, string Model, string SerialNumber, string Camera,
    decimal AutonomyMinutes, decimal Battery, string Status, string ImageUrl, DateTimeOffset? CreatedAt);
