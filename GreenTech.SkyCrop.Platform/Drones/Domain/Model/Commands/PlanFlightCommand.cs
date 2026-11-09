namespace GreenTech.SkyCrop.Platform.Drones.Domain.Model.Commands;

public sealed record PlanFlightCommand(int DroneId, int FlightRouteId, int PlotId, string Status = "PLANNED");
