using GreenTech.SkyCrop.Platform.Reports.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Reports.Domain.Model.Commands;
using GreenTech.SkyCrop.Platform.Shared.Application.Model;

namespace GreenTech.SkyCrop.Platform.Reports.Application.CommandServices;

public interface IReportCommandService
{
    
    Task<Result<Report>> Handle(CreateReportCommand command, CancellationToken cancellationToken);
    
    
}