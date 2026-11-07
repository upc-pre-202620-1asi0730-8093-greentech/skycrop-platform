using GreenTech.SkyCrop.Platform.Shared.Domain.Model.ValueObjects;

namespace GreenTech.SkyCrop.Platform.Reports.Domain.Model.ValueObjects;

public readonly record struct MetricsId(int Value) : IEntityId<int>
{
    public static implicit operator int(MetricsId id) => id.Value;
    public static implicit operator MetricsId(int value) => new(value);

    public override string ToString() => Value.ToString();
}