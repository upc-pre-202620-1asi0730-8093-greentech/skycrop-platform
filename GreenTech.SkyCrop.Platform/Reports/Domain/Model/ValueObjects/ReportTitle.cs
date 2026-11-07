namespace GreenTech.SkyCrop.Platform.Reports.Domain.Model.ValueObjects;

public record ReportTitle
{
    public string Name { get; }
    
    public ReportTitle(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }
};