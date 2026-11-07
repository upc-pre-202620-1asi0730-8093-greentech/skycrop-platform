using GreenTech.SkyCrop.Platform.Shared.Domain.Model.ValueObjects;

namespace GreenTech.SkyCrop.Platform.Reports.Domain.Model.ValueObjects;

public readonly record struct PerformanceId(int Value) : IEntityId<int>
{
    public static implicit operator int(PerformanceId id) => id.Value;
    public static implicit operator PerformanceId(int value) => new(value);

    public override string ToString() => Value.ToString();
}