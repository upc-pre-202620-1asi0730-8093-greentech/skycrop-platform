using GreenTech.SkyCrop.Platform.Shared.Domain.Model.ValueObjects;

namespace GreenTech.SkyCrop.Platform.Reports.Domain.Model.ValueObjects;

/// <summary>
/// Represents the identifier of a report in the Reports bounded context
/// </summary>
/// <param name="Value">The value of the identifier</param>
public readonly record struct ReportId(int Value) : IEntityId<int>
{
    public static implicit operator int(ReportId id) => id.Value;
    public static implicit operator ReportId(int value) => new(value);

    public override string ToString() => Value.ToString();
}