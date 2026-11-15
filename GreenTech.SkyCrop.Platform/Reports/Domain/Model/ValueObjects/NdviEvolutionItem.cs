namespace GreenTech.SkyCrop.Platform.Reports.Domain.Model.ValueObjects;

/// <summary>
/// Date and value object representing the average NDVI at a specific point in time
/// </summary>
public record NdviEvolutionItem
{
    public DateTimeOffset Date { get; }
    public float Value { get; }
    public NdviEvolutionItem(DateTimeOffset date, float value)
    {
        if (float.IsNaN(Value) || float.IsInfinity(Value) || Value < -1 || Value > 1)
            throw new ArgumentOutOfRangeException(nameof(Value), @"Value must be between -1 and 1.");
        
        Date = date;
        Value = value;
    }

    public static NdviEvolutionItem FromDictionary(Dictionary<string, object> item)
    {
        if (!item.TryGetValue("date", out var dateObj))
            throw new ArgumentException("Date not found");
        
        if (!item.TryGetValue("value", out var valueObj))
            throw new ArgumentException("Value not found");

        var parsedDate = DateTimeOffset.Parse(dateObj.ToString()!);
        var parsedValue = Convert.ToSingle(valueObj);
        
        return new NdviEvolutionItem(parsedDate, parsedValue);
    }
}