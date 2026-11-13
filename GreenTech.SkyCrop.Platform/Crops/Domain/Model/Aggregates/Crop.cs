using GreenTech.SkyCrop.Platform.Crops.Domain.Model.Commands;
using GreenTech.SkyCrop.Platform.Crops.Domain.Model.ValueObjects;
using GreenTech.SkyCrop.Platform.Shared.Domain.Model.Aggregates;

namespace GreenTech.SkyCrop.Platform.Crops.Domain.Model.Aggregates;

public partial class Crop : AggregateRoot
{
    private const int TextLength = 120;
    private const int UrlLength = 2048;
    private const decimal HectaresArea = 999999999999.999999m;
    private const int Zero = 0;
    private const int DecimalMax = 6;
    private const decimal MaxHumidity = 100;
    
    private Crop() {}
    
    public Crop(CreateCropCommand command)
    {
        Name = RequiredText(command.Name, nameof(command.Name), TextLength);
        Variety = RequiredText(command.Variety, nameof(command.Variety), TextLength);
        SowingMethod = RequiredText(command.SowingMethod, nameof(command.SowingMethod), TextLength);
        IrrigationType = RequiredText(command.IrrigationType, nameof(command.IrrigationType), TextLength);
        SoilType = RequiredText(command.SoilType, nameof(command.SoilType), TextLength);
        Notes = RequiredText(command.Notes, nameof(command.Notes), TextLength);
        ImageURl = RequiredText(command.ImageUrl, nameof(command.ImageUrl), UrlLength);
        GrowthStage = RequiredText(command.GrowthStage, nameof(command.GrowthStage), TextLength);
        
        if (command.PlantedAreaHectares <= Zero || command.PlantedAreaHectares > HectaresArea ||
            decimal.Round(command.PlantedAreaHectares, DecimalMax) != command.PlantedAreaHectares )
            throw new ArgumentException($"Area must be positive, fit in decimal(18,6), and have at most six decimal places.", nameof(command.PlantedAreaHectares));
        PlantedAreaHectares = command.PlantedAreaHectares;
        
        if (command.PlantingDate == default)
            throw new ArgumentException($"A planting date is required.", nameof(command.PlantingDate));
        PlantingDate = command.PlantingDate;
        
        if (command.IdealHumidityMin < Zero || command.IdealHumidityMin > MaxHumidity)
            throw new ArgumentException($"Ideal Minimum Humidity must be positive and not exceed 100",
                nameof(command.IdealHumidityMin));
        IdealHumidityMin = command.IdealHumidityMin;
        if (command.IdealHumidityMax < Zero || command.IdealHumidityMax > MaxHumidity)
            throw new ArgumentException($"Ideal Maximum Humidity must be positive and not exceed 100", 
                nameof(command.IdealHumidityMax));
        IdealHumidityMax = command.IdealHumidityMax;
    }

    public CropId Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string Variety { get; private set; } = null!;
    public DateOnly PlantingDate { get; private set; }
    public DateOnly? ExpectedHarvest { get; private set; } = null!;
    public CropStatus Status { get; private set; } = CropStatus.Active;
    public decimal PlantedAreaHectares { get; private set; }
    public string SowingMethod { get; private set; } = null!;
    public string IrrigationType { get; private set; } = null!;
    public string SoilType { get; private set; } = null!;
    public string Notes { get; private set; } = null!;
    public string ImageURl { get; private set; } = null!;
    public string GrowthStage { get; private set; } = null!;
    public decimal IdealHumidityMin { get; private set; }
    public decimal IdealHumidityMax { get; private set; }
    
    private static string RequiredText(string? value, string field, int maxLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, field);
        var text = value.Trim();
        if (text.Length > maxLength)
            throw new ArgumentException($"Use at most {maxLength} characters.", field);
        return text;
    }
}