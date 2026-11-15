namespace GreenTech.SkyCrop.Platform.Drones.Domain.Model.Errors;

/// <summary>A valid request conflicts with the current aggregate state.</summary>
public sealed class DroneStateException(string message) : Exception(message);
