using GreenTech.SkyCrop.Platform.Reports.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Reports.Domain.Model.Commands;
using GreenTech.SkyCrop.Platform.Shared.Application.Model;

namespace GreenTech.SkyCrop.Platform.Reports.Application.CommandServices;

/// <summary>
/// Report command service interface
/// </summary>
public interface IReportCommandService
{
    
    /// <summary>
    /// Handles the Create Report command
    /// </summary>
    Task<Result<Report>> Handle(CreateReportCommand command, CancellationToken cancellationToken);
    
    
}