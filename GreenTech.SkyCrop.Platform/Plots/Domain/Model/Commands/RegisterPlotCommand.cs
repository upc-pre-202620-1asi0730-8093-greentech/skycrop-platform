namespace GreenTech.SkyCrop.Platform.Plots.Domain.Model.Commands;

public sealed record RegisterPlotCommand(
    string? Name,
    string? PersonInCharge,
    string? CropType,
    string? SoilType,
    DateOnly SowingDate,
    decimal AreaHectares,
    string? Location,
    double? Latitude,
    double? Longitude,
    string? ImageUrl);
