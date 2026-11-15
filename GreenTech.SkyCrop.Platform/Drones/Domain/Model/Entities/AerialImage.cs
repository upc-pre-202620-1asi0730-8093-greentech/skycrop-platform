using GreenTech.SkyCrop.Platform.Drones.Domain.Model.ValueObjects;

namespace GreenTech.SkyCrop.Platform.Drones.Domain.Model.Entities;

/// <summary>Persisted image metadata. Images are supplied by a future capture integration.</summary>
public sealed class AerialImage
{
    private AerialImage() { }
    public AerialImage(int flightId, string fileUrl, DateTimeOffset capturedAt)
    {
        if (flightId <= 0) throw new ArgumentException("A positive flight identifier is required.");
        FlightId = flightId;
        FileUrl = DroneValidation.HttpUrl(fileUrl, false);
        CapturedAt = capturedAt.ToUniversalTime();
    }
    public int Id { get; private set; }
    public int FlightId { get; private set; }
    public string FileUrl { get; private set; } = null!;
    public DateTimeOffset CapturedAt { get; private set; }
}
