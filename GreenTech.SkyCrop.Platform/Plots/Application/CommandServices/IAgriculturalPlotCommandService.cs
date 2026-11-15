using GreenTech.SkyCrop.Platform.Plots.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Plots.Domain.Model.Commands;
using GreenTech.SkyCrop.Platform.Shared.Application.Model;

namespace GreenTech.SkyCrop.Platform.Plots.Application.CommandServices;

public interface IAgriculturalPlotCommandService
{
    Task<Result<AgriculturalPlot>> Handle(RegisterPlotCommand command, CancellationToken cancellationToken);
}
