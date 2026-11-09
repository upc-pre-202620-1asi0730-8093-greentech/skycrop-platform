namespace GreenTech.SkyCrop.Platform.Drones.Domain.Model.Commands;

public sealed record CancelFlightCommand(int FlightId, string Status = "CANCELLED");
