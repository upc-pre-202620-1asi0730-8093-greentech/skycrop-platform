using GreenTech.SkyCrop.Platform.Crops.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Crops.Interfaces.Rest.Resources;

namespace GreenTech.SkyCrop.Platform.Crops.Interfaces.Rest.Transform;

public static class CropResourceFromEntityAssembler
{
    public static CropResource ToResourceFromEntity(Crop entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new CropResource(
            entity.Id, 
            entity.Name, 
            entity.Variety, 
            entity.PlantingDate, 
            entity.PlantedAreaHectares,
            entity.SowingMethod,
            entity.GrowthStage,
            entity.Notes
            );
    }
}