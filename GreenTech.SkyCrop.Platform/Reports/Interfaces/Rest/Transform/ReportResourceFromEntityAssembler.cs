using GreenTech.SkyCrop.Platform.Reports.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Reports.Interfaces.Rest.Resources;

namespace GreenTech.SkyCrop.Platform.Reports.Interfaces.Rest.Transform;

public static class ReportResourceFromEntityAssembler
{

    public static ReportResource ToResourceFromEntity(Report entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        ReportMetricsResource metrics = new ReportMetricsResource(
            entity.ReportMetrics.Flights,
            entity.ReportMetrics.Alerts,
            entity.ReportMetrics.AverageNdvi,
            entity.ReportMetrics.AreaHectares
        );

        List<NdviEvolutionItemResource> ndviEvolution = entity.NdviEvolution
            .Select(n => new NdviEvolutionItemResource(n.Date.ToString(), n.Value)).ToList();

        List<ReportPerformanceItemResource> performance = entity.Performance
            .Select(p => new ReportPerformanceItemResource(p.PlotId, p.Alerts, p.Flights, p.AverageNdvi)).ToList();

        return new ReportResource(
            entity.Id,
            entity.Title.Value,
            entity.GeneratedAt.ToString(),
            entity.Season.ToString(),
            entity.Year,
            entity.Owner.Name,
            entity.GeneratedBy.ToString(),
            entity.Type.ToString(),
            metrics,
            ndviEvolution,
            performance
        );
    }
}