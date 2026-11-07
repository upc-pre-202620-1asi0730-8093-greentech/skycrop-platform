using GreenTech.SkyCrop.Platform.Shared.Domain.Model.ValueObjects;

namespace GreenTech.SkyCrop.Platform.Reports.Domain.Model.ValueObjects;

public readonly record struct NdviEvolutionId(int Value) : IEntityId<int>
{
    public static implicit operator int(NdviEvolutionId id) => id.Value;
    public static implicit operator NdviEvolutionId(int value) => new(value);

    public override string ToString() => Value.ToString();
}

