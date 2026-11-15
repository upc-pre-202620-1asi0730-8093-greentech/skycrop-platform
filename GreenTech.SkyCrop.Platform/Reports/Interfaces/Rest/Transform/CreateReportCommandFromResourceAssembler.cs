using GreenTech.SkyCrop.Platform.Reports.Domain.Model.Commands;
using GreenTech.SkyCrop.Platform.Reports.Interfaces.Rest.Resources;

namespace GreenTech.SkyCrop.Platform.Reports.Interfaces.Rest.Transform;

/// <summary>
/// Assembler responsible for transforming a CreateReportResource into a CreateReportCommand
/// </summary>
public static class CreateReportCommandFromResourceAssembler
{

    public static CreateReportCommand ToCommandFromResource(CreateReportResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        var ndviEvolution = resource.NdviEvolution.Select(n => new Dictionary<string, object>
        {
            { "date", n.Date },
            { "value", n.Value }
        }).ToList();


        var performance = resource.Performance.Select(p => new Dictionary<string, object>
        {
            { "plotId", p.PlotId },
            { "alerts", p.Alerts },
            { "flights", p.Flights },
            { "averageNdvi", p.AverageNdvi },
        }).ToList();

        return new CreateReportCommand(
            resource.Title,
            resource.GeneratedAt,
            resource.Season,
            resource.Year,
            resource.Owner,
            resource.GeneratedBy,
            resource.Type,
            resource.Flights,
            resource.Alerts,
            resource.AverageNdvi,
            resource.AreaHectares,
            ndviEvolution,
            performance
        );
    }
}