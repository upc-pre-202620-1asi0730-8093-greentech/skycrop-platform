namespace GreenTech.SkyCrop.Platform.Drones.Domain.Model.ValueObjects;

/// <summary>Flight height in meters and speed in meters per second.</summary>
public sealed record FlightParameters
{
    public FlightParameters(decimal altitudeMeters, decimal speedMetersPerSecond)
    {
        DroneValidation.PositiveMeasurement(altitudeMeters, nameof(altitudeMeters));
        DroneValidation.PositiveMeasurement(speedMetersPerSecond, nameof(speedMetersPerSecond));
        AltitudeMeters = altitudeMeters;
        SpeedMetersPerSecond = speedMetersPerSecond;
    }
    public decimal AltitudeMeters { get; }
    public decimal SpeedMetersPerSecond { get; }
}
