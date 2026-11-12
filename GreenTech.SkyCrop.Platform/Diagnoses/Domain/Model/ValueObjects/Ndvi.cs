namespace GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.ValueObjects;

public sealed record Ndvi
{
    public double Value { get; }

    public Ndvi(double value)
    {
        if (!double.IsFinite(value) || value < -1 || value > 1)
        {
            throw new ArgumentException(
                "NDVI must be a finite number between -1 and 1.",
                nameof(value));
        }

        Value = value;
    }
}