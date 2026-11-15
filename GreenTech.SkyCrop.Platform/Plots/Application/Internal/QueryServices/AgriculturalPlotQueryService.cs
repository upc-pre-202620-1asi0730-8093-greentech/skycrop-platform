using GreenTech.SkyCrop.Platform.Plots.Application.QueryServices;
using GreenTech.SkyCrop.Platform.Plots.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Plots.Domain.Model.Queries;
using GreenTech.SkyCrop.Platform.Plots.Domain.Repositories;

namespace GreenTech.SkyCrop.Platform.Plots.Application.Internal.QueryServices;

public sealed class AgriculturalPlotQueryService(IAgriculturalPlotRepository repository) : IAgriculturalPlotQueryService
{
    public async Task<IEnumerable<AgriculturalPlot>> Handle(GetAllPlotsQuery query, CancellationToken cancellationToken) =>
        (await repository.ListAsNoTrackingAsync(cancellationToken)).OrderBy(plot => plot.Id);

    public Task<AgriculturalPlot?> Handle(GetPlotByIdQuery query, CancellationToken cancellationToken) =>
        repository.FindByIdAsNoTrackingAsync(query.Id, cancellationToken);
}
