using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Repositories;
using GreenTech.SkyCrop.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using GreenTech.SkyCrop.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GreenTech.SkyCrop.Platform.Diagnoses.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
/// Persists diagnosis aggregates through the shared database context.
/// Changes are committed by the unit of work.
/// </summary>
public sealed class DiagnosisRepository(AppDbContext context)
    : BaseRepository<Diagnosis>(context), IDiagnosisRepository
{
    public async Task<IEnumerable<Diagnosis>> FindByPlotIdAsync(
        int plotId,
        CancellationToken cancellationToken = default)
    {
        if (plotId <= 0)
            throw new ArgumentException(
                "A positive plot identifier is required.",
                nameof(plotId));

        return await Context.Set<Diagnosis>()
            .AsNoTracking()
            .Include(diagnosis => diagnosis.Anomalies)
            .Where(diagnosis => diagnosis.PlotId == plotId)
            .OrderByDescending(diagnosis => diagnosis.MonitoredAt)
            .ThenByDescending(diagnosis => diagnosis.Id)
            .ToListAsync(cancellationToken);
    }
}