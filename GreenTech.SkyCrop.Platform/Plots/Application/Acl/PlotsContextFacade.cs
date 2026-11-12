using GreenTech.SkyCrop.Platform.Plots.Application.QueryServices;
using GreenTech.SkyCrop.Platform.Plots.Domain.Model.Queries;
using GreenTech.SkyCrop.Platform.Plots.Interfaces.Acl;

namespace GreenTech.SkyCrop.Platform.Plots.Application.Acl;

internal sealed class PlotsContextFacade(IAgriculturalPlotQueryService queries) : IPlotsContextFacade
{
    public async Task<bool> PlotExistsAsync(int plotId, CancellationToken cancellationToken) =>
        await queries.Handle(new GetPlotByIdQuery(plotId), cancellationToken) is not null;
}
