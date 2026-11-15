using GreenTech.SkyCrop.Platform.Drones.Domain.Model.Commands;
using GreenTech.SkyCrop.Platform.Drones.Domain.Model.Entities;
using GreenTech.SkyCrop.Platform.Drones.Domain.Model.ValueObjects;
using GreenTech.SkyCrop.Platform.Shared.Domain.Model.Aggregates;

namespace GreenTech.SkyCrop.Platform.Drones.Domain.Model.Aggregates;

public sealed class FlightRoute : AggregateRoot
{
    private readonly List<RouteWaypoint> _waypoints = [];
    private FlightRoute() { }
    public FlightRoute(CreateFlightRouteCommand command) => SetRoute(command.Name, command.PlotId,
        command.AltitudeMeters, command.SpeedMetersPerSecond, command.Waypoints);
    public int Id { get; private set; }
    public string Name { get; private set; } = null!;
    public int PlotId { get; private set; }
    public FlightParameters Parameters { get; private set; } = null!;
    public IReadOnlyCollection<RouteWaypoint> Waypoints => _waypoints.AsReadOnly();
    public Guid Version { get; private set; } = Guid.NewGuid();

    public void Update(UpdateFlightRouteCommand command) => SetRoute(command.Name ?? Name,
        command.PlotId ?? PlotId, command.AltitudeMeters ?? Parameters.AltitudeMeters,
        command.SpeedMetersPerSecond ?? Parameters.SpeedMetersPerSecond,
        command.Waypoints ?? Waypoints.OrderBy(point => point.Sequence).ToArray());
    public void Touch() => Version = Guid.NewGuid();

    private void SetRoute(string name, int plotId, decimal altitude, decimal speed, IReadOnlyList<RouteWaypoint>? points)
    {
        var nextName = DroneValidation.RequiredText(name, nameof(name));
        if (plotId <= 0) throw new ArgumentException("A positive plot identifier is required.");
        var parameters = new FlightParameters(altitude, speed);
        if (points is null || points.Count < 2 || points.Any(p => p is null))
            throw new ArgumentException("A route requires at least two valid waypoints.");
        var nextPoints = points.Select((point, index) => new RouteWaypoint(index, point.Latitude, point.Longitude)).ToArray();
        Name = nextName;
        PlotId = plotId;
        Parameters = parameters;
        _waypoints.Sort((left, right) => left.Sequence.CompareTo(right.Sequence));
        // Retain owned entity keys when replacing coordinates at the same positions.
        for (var index = 0; index < Math.Min(_waypoints.Count, nextPoints.Length); index++)
            _waypoints[index].Relocate(nextPoints[index]);
        if (_waypoints.Count > nextPoints.Length)
            _waypoints.RemoveRange(nextPoints.Length, _waypoints.Count - nextPoints.Length);
        else
            _waypoints.AddRange(nextPoints.Skip(_waypoints.Count));
        Touch();
    }
}
