namespace GreenTech.SkyCrop.Platform.Drones.Domain.Model.Commands;

public sealed record UpdateDroneCommand(int DroneId, string? Name = null, string? Model = null, string? SerialNumber = null, string? Camera = null, decimal? AutonomyMinutes = null, decimal? Battery = null, string? ImageUrl = null, string? Status = null);
