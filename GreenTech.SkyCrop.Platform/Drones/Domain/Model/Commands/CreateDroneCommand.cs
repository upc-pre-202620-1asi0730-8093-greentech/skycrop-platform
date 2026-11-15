namespace GreenTech.SkyCrop.Platform.Drones.Domain.Model.Commands;

public sealed record CreateDroneCommand(string Name, string Model, string SerialNumber, string Camera, decimal AutonomyMinutes, decimal Battery = 0, string? ImageUrl = null, string Status = "DISCONNECTED");
