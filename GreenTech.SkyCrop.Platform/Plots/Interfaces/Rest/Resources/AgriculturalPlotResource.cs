namespace GreenTech.SkyCrop.Platform.Plots.Interfaces.Rest.Resources;

public sealed record AgriculturalPlotResource(
    int Id, string Name, string PersonInCharge, string CropType, string SoilType, DateOnly SowingDate,
    decimal AreaHectares, string Location, double? Latitude, double? Longitude, string ImageUrl,
    string Status, DateTimeOffset? CreatedAt);
