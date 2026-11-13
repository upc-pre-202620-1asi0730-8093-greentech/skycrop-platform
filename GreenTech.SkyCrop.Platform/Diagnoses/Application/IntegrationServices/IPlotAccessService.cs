using GreenTech.SkyCrop.Platform.Shared.Application.Model;

namespace GreenTech.SkyCrop.Platform.Diagnoses.Application.IntegrationServices;

/// <summary>
/// Checks plot access for the authenticated caller.
/// Implementations must obtain identity from trusted authentication context,
/// never from a user identifier supplied in a request body.
/// </summary>
public interface IPlotAccessService
{
    /// <summary>
    /// Returns success only when the caller can access the requested plot.
    /// </summary>
    Task<Result> CheckAccessAsync(
        int plotId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the plot identifiers accessible to the authenticated caller.
    /// An empty successful result means the caller has no accessible plots.
    /// A failure means access could not be determined.
    /// </summary>
    Task<Result<IReadOnlyCollection<int>>> GetAccessiblePlotIdsAsync(
        CancellationToken cancellationToken = default);
}