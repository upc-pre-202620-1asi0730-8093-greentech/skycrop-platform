namespace GreenTech.SkyCrop.Platform.Reports.Domain.Model.ValueObjects;

/// <summary>
/// The name of a person
/// </summary>
public record PersonName
{
    public string Name { get; }
    
    public PersonName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }
};