using GreenTech.SkyCrop.Platform.Shared.Domain.Model.ValueObjects;

namespace GreenTech.SkyCrop.Platform.Reports.Domain.Model.ValueObjects;

public readonly record struct PerfomanceId(int Value) : IEntityId<int>
{
    public static implicit operator int(PerfomanceId id) => id.Value;
    public static implicit operator PerfomanceId(int value) => new(value);

    public override string ToString() => Value.ToString();
}