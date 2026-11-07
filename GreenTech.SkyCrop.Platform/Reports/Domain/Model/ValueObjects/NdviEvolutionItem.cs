namespace GreenTech.SkyCrop.Platform.Reports.Domain.Model.ValueObjects;

public record NdviEvolutionItem
{
    public DateTimeOffset Date { get; }
    public float Value { get; }

    public NdviEvolutionItem(Dictionary<string, object> item)
    {
        if (!item.TryGetValue("date", out var date))
            throw new ArgumentException("Date not found");
        
        if (!item.TryGetValue("value", out var value))
            throw new ArgumentException("Value not found");

        Date = DateTimeOffset.Parse(date.ToString()!);
        Value = Convert.ToSingle(value);
        
        if (float.IsNaN(Value) || float.IsInfinity(Value) || Value < -1 || Value > 1)
            throw new ArgumentOutOfRangeException(nameof(Value), @"Value must be between -1 and 1.");
    }
};