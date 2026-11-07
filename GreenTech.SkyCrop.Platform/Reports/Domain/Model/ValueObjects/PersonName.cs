namespace GreenTech.SkyCrop.Platform.Reports.Domain.Model.ValueObjects;

public record PersonName
{
    public string Name { get; }
    
    public PersonName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }
};