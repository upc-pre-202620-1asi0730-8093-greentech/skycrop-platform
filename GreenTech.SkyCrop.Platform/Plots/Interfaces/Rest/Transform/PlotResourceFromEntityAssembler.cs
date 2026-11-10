using GreenTech.SkyCrop.Platform.Plots.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Plots.Interfaces.Rest.Resources;

namespace GreenTech.SkyCrop.Platform.Plots.Interfaces.Rest.Transform;

public static class PlotResourceFromEntityAssembler
{
    public static AgriculturalPlotResource ToResourceFromEntity(AgriculturalPlot plot) =>
        new(plot.Id, plot.Name, plot.PersonInCharge, plot.CropType, plot.SoilType, plot.SowingDate,
            plot.AreaHectares, plot.Location, plot.Latitude, plot.Longitude, plot.ImageUrl, plot.Status, plot.CreatedAt);
}
