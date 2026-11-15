using GreenTech.SkyCrop.Platform.Drones.Domain.Model.Errors;
using GreenTech.SkyCrop.Platform.Shared.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Shared.Domain.Model.Entities;

namespace GreenTech.SkyCrop.Platform.Drones.Domain.Model.Aggregates;

public sealed class Flight : AggregateRoot, IAuditableEntity
{
    private Flight() { }
    public Flight(Drone drone, FlightRoute route, int plotId)
    {
        if (drone.Status != EDroneStatus.AVAILABLE)
            throw new DroneStateException("The drone must be AVAILABLE before planning a flight.");
        if (drone.Id <= 0 || route.Id <= 0 || plotId <= 0 || route.PlotId != plotId)
            throw new ArgumentException("The flight must reference the route's plot and a persisted drone and route.");
        DroneId = drone.Id;
        FlightRouteId = route.Id;
        PlotId = plotId;
        ActiveDroneId = drone.Id;
        drone.Touch();
        route.Touch();
    }
    public int Id { get; private set; }
    public int DroneId { get; private set; }
    public int FlightRouteId { get; private set; }
    public int PlotId { get; private set; }
    public EFlightStatus Status { get; private set; } = EFlightStatus.PLANNED;
    // A unique nullable index enforces one active flight per drone, including concurrent requests.
    public int? ActiveDroneId { get; private set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public decimal? DurationMinutes { get; private set; }
    public Guid Version { get; private set; } = Guid.NewGuid();

    public void Cancel()
    {
        if (Status != EFlightStatus.PLANNED)
            throw new DroneStateException("Only PLANNED flights can be cancelled.");
        Status = EFlightStatus.CANCELLED;
        ActiveDroneId = null;
        Version = Guid.NewGuid();
    }
}
