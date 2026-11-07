namespace GreenTech.SkyCrop.Platform.Reports.Domain.Model.ValueObjects;

public record DroneName
{
    public string Name { get; }
    
    public DroneName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }
};