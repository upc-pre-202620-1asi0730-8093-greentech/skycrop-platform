namespace GreenTech.SkyCrop.Platform.Crops.Interfaces.Rest.Resources;

public record CreateCropResource(
    string Name,
    string Variety,
    DateOnly PlantingDate,
    decimal PlantedAreaHectares,
    string SowingMethod,
    string IrrigationType,
    string SoilType,
    string Notes,
    string ImageUrl,
    string GrowthStage,
    decimal IdealHumidityMin,
    decimal IdealHumidityMax
    );