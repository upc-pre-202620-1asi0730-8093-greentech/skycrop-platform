using GreenTech.SkyCrop.Platform.Plots.Application.CommandServices;
using GreenTech.SkyCrop.Platform.Plots.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Plots.Domain.Model.Commands;
using GreenTech.SkyCrop.Platform.Plots.Domain.Model.Errors;
using GreenTech.SkyCrop.Platform.Plots.Domain.Repositories;
using GreenTech.SkyCrop.Platform.Shared.Application.Model;
using GreenTech.SkyCrop.Platform.Shared.Domain.Repositories;

namespace GreenTech.SkyCrop.Platform.Plots.Application.Internal.CommandServices;

public sealed class AgriculturalPlotCommandService(IAgriculturalPlotRepository repository, IUnitOfWork unitOfWork)
    : IAgriculturalPlotCommandService
{
    public async Task<Result<AgriculturalPlot>> Handle(RegisterPlotCommand command, CancellationToken cancellationToken)
    {
        AgriculturalPlot plot;
        try
        {
            plot = new AgriculturalPlot(command);
        }
        catch (ArgumentException exception)
        {
            return Result<AgriculturalPlot>.Failure(PlotErrors.InvalidData(exception.Message));
        }

        await repository.AddAsync(plot, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result<AgriculturalPlot>.Success(plot);
    }
}
