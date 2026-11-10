using GreenTech.SkyCrop.Platform.Plots.Domain.Model.Commands;
using GreenTech.SkyCrop.Platform.Plots.Interfaces.Rest.Resources;

namespace GreenTech.SkyCrop.Platform.Plots.Interfaces.Rest.Transform;

public static class PlotCommandFromResourceAssembler
{
    public static RegisterPlotCommand ToCommandFromResource(RegisterPlotResource resource) =>
        new(resource.Name, resource.PersonInCharge, resource.CropType, resource.SoilType, resource.SowingDate,
            resource.AreaHectares, resource.Location, resource.Latitude, resource.Longitude, resource.ImageUrl);
}
