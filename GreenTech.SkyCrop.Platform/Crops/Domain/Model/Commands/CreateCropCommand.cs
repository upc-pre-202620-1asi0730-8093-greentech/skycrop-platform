namespace GreenTech.SkyCrop.Platform.Crops.Domain.Model.Commands;

public record CreateCropCommand(    
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
    decimal IdealHumidityMax);