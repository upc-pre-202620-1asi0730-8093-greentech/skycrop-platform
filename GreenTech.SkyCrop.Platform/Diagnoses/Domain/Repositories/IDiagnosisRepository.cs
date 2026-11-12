using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Shared.Domain.Repositories;

namespace GreenTech.SkyCrop.Platform.Diagnoses.Domain.Repositories;

/// <summary>
/// Defines persistence operations for diagnosis aggregates.
/// </summary>
public interface IDiagnosisRepository : IBaseRepository<Diagnosis>
{
    /// <summary>
    /// Returns the diagnosis history for a plot without change tracking,
    /// including anomalies and image identifiers.
    /// Results are ordered by MonitoredAt descending, then Id descending.
    /// Returns an empty collection when the plot has no diagnoses.
    /// </summary>
    Task<IEnumerable<Diagnosis>> FindByPlotIdAsync(
        int plotId,
        CancellationToken cancellationToken = default);
}