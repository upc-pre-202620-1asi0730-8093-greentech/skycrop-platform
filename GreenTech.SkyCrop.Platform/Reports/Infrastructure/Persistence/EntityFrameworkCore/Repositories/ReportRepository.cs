using GreenTech.SkyCrop.Platform.Reports.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Reports.Domain.Model.ValueObjects;
using GreenTech.SkyCrop.Platform.Reports.Domain.Repositories;
using GreenTech.SkyCrop.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using GreenTech.SkyCrop.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GreenTech.SkyCrop.Platform.Reports.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
/// Report repository implementation
/// </summary>
/// <param name="context">The database context</param>
public class ReportRepository(AppDbContext context) : BaseRepository<Report>(context), IReportRepository
{
    /// <inheritdoc/>
    public async Task<Report?> FindReportByIdAsync(ReportId reportId, CancellationToken cancellationToken)
    {
        return await Context.Set<Report>().FirstOrDefaultAsync(r => r.Id == reportId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Report?> FindReportByIdAsNoTrackingAsync(ReportId reportId, CancellationToken cancellationToken)
    {
        return await Context.Set<Report>().AsNoTracking().FirstOrDefaultAsync(r => r.Id == reportId, cancellationToken);
    }
}