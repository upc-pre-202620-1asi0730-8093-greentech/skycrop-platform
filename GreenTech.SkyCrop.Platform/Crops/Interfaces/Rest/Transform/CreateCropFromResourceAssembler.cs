using GreenTech.SkyCrop.Platform.Crops.Domain.Model.Commands;
using GreenTech.SkyCrop.Platform.Crops.Interfaces.Rest.Resources;

namespace GreenTech.SkyCrop.Platform.Crops.Interfaces.Rest.Transform;

public static class CreateCropFromResourceAssembler
{
    public static CreateCropCommand ToCommandFromResource(CreateCropResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);
        return new CreateCropCommand(
            resource.Name,
            resource.Variety,
            resource.PlantingDate,
            resource.PlantedAreaHectares,
            resource.SowingMethod,
            resource.IrrigationType,
            resource.SoilType,
            resource.Notes,
            resource.ImageUrl,
            resource.GrowthStage,
            resource.IdealHumidityMin,
            resource.IdealHumidityMax
        );
    }
}