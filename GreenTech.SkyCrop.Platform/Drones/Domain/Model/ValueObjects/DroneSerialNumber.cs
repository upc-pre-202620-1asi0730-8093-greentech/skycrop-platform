namespace GreenTech.SkyCrop.Platform.Drones.Domain.Model.ValueObjects;

/// <summary>A serial number with a case-insensitive uniqueness key.</summary>
public sealed record DroneSerialNumber
{
    public DroneSerialNumber(string value)
    {
        Value = DroneValidation.RequiredText(value, nameof(value), 120);
    }
    public string Value { get; }
    public string Key => Value.ToUpperInvariant();
}
