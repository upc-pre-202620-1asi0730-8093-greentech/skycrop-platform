using GreenTech.SkyCrop.Platform.Plots.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Plots.Domain.Model.Queries;

namespace GreenTech.SkyCrop.Platform.Plots.Application.QueryServices;

public interface IAgriculturalPlotQueryService
{
    Task<IEnumerable<AgriculturalPlot>> Handle(GetAllPlotsQuery query, CancellationToken cancellationToken);
    Task<AgriculturalPlot?> Handle(GetPlotByIdQuery query, CancellationToken cancellationToken);
}
