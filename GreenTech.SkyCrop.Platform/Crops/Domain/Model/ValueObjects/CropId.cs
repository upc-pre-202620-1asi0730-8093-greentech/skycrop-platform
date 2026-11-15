using GreenTech.SkyCrop.Platform.Shared.Domain.Model.ValueObjects;

namespace GreenTech.SkyCrop.Platform.Crops.Domain.Model.ValueObjects;

public readonly record struct CropId(int Value) : IEntityId<int>
{
    public static implicit operator int(CropId id) => id.Value;
    public static implicit operator CropId(int value) => new(value);
    
    public override string ToString() => Value.ToString();
}