using GreenTech.SkyCrop.Platform.Reports.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Reports.Domain.Model.ValueObjects;
using GreenTech.SkyCrop.Platform.Shared.Domain.Repositories;

namespace GreenTech.SkyCrop.Platform.Reports.Domain.Repositories;

/// <summary>
/// Report repository interface
/// </summary>
public interface IReportRepository : IBaseRepository<Report>
{
    /// <summary>
    /// Find a report by its identifier
    /// </summary>
    Task<Report?> FindReportByIdAsync(ReportId reportId, CancellationToken cancellationToken);
    
    /// <summary>
    /// Find a report by its identifier without change tracking
    /// </summary>
    Task<Report?> FindReportByIdAsNoTrackingAsync(ReportId reportId, CancellationToken cancellationToken);
}