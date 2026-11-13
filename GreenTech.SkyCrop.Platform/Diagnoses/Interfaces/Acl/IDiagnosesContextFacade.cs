using GreenTech.SkyCrop.Platform.Shared.Application.Model;

namespace GreenTech.SkyCrop.Platform.Diagnoses.Interfaces.Acl;

public interface IDiagnosesContextFacade
{
    /// <summary>
    /// Returns the most recent completed diagnosis by monitoring time.
    /// Equal monitoring times are resolved by diagnosis ID descending.
    /// A successful null result means no completed diagnosis exists.
    /// Access failures are preserved.
    /// </summary>
    Task<Result<DiagnosisSummary?>> GetLatestCompletedByPlotIdAsync(
        int plotId,
        CancellationToken cancellationToken = default);
}