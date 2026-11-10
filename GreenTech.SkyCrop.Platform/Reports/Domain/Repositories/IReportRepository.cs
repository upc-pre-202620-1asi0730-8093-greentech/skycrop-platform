using GreenTech.SkyCrop.Platform.Reports.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Reports.Domain.Model.ValueObjects;
using GreenTech.SkyCrop.Platform.Shared.Domain.Repositories;

namespace GreenTech.SkyCrop.Platform.Reports.Domain.Repositories;

public interface IReportRepository : IBaseRepository<Report>
{
    Task<Report?> FindReportByIdAsync(ReportId reportId, CancellationToken cancellationToken);
    
    Task<Report?> FindReportByIdAsNoTrackingAsync(ReportId reportId, CancellationToken cancellationToken);
}