using GreenTech.SkyCrop.Platform.Shared.Domain.Model.ValueObjects;

namespace GreenTech.SkyCrop.Platform.Reports.Domain.Model.ValueObjects;

public readonly record struct ReportId(int Value) : IEntityId<int>
{
    public static implicit operator int(ReportId id) => id.Value;
    public static implicit operator ReportId(int value) => new(value);

    public override string ToString() => Value.ToString();
}