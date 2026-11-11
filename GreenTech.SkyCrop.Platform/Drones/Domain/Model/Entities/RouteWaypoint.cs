namespace GreenTech.SkyCrop.Platform.Drones.Domain.Model.Entities;

/// <summary>An ordered, immutable geographic coordinate in a flight route.</summary>
public sealed class RouteWaypoint
{
    public RouteWaypoint(int sequence, double latitude, double longitude)
    {
        if (sequence < 0 || !double.IsFinite(latitude) || !double.IsFinite(longitude) ||
            latitude is < -90 or > 90 || longitude is < -180 or > 180)
            throw new ArgumentException("Waypoints require valid latitude and longitude coordinates.");
        Sequence = sequence;
        Latitude = latitude;
        Longitude = longitude;
    }
    internal void Relocate(RouteWaypoint point)
    {
        Latitude = point.Latitude;
        Longitude = point.Longitude;
    }
    public int Sequence { get; private set; }
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
}
