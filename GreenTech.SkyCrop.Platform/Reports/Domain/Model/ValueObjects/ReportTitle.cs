namespace GreenTech.SkyCrop.Platform.Reports.Domain.Model.ValueObjects;

public record ReportTitle
{
    public string Value { get; }
    
    public ReportTitle(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }
};