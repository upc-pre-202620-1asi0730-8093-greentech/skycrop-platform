namespace GreenTech.SkyCrop.Platform.Drones.Interfaces.Rest.Resources;

public sealed record AerialImageResource(
    int Id, int FlightId, string ImageUrl, DateTimeOffset CapturedAt);
