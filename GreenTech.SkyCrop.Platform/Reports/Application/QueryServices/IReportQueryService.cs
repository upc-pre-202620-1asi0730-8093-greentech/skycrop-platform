using GreenTech.SkyCrop.Platform.Reports.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Reports.Domain.Model.Queries;

namespace GreenTech.SkyCrop.Platform.Reports.Application.QueryServices;

/// <summary>
/// Report Query Service Interface
/// </summary>
public interface IReportQueryService
{
    /// <summary>
    /// Handles the Get Report By ID query
    /// </summary>
    Task<Report?> Handle(GetReportByIdQuery query, CancellationToken cancellationToken);
    
    /// <summary>
    /// Handles the Get All Reports query
    /// </summary>
    Task<IEnumerable<Report>> Handle(GetAllReportsQuery query, CancellationToken cancellationToken);
}