namespace GreenTech.SkyCrop.Platform.Crops.Interfaces.Rest.Resources;

public record CropResource(
    int Id,
    string Name,
    string Variety,
    DateOnly PlantingDate,
    decimal PlantedAreaHectares,
    string SowingMethod,
    string GrowthStage,
    string Notes
    );