namespace GreenTech.SkyCrop.Platform.Drones.Domain.Model.ValueObjects;

public sealed record BatteryLevel
{
    public BatteryLevel(decimal value)
    {
        if (value is < 0 or > 100 || decimal.Round(value, 6) != value) throw new ArgumentException("Battery must be between 0 and 100 percent with at most six decimal places.");
        Value = value;
    }
    public decimal Value { get; }
}
