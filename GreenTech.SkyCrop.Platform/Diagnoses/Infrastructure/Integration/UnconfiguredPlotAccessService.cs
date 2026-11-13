using GreenTech.SkyCrop.Platform.Diagnoses.Application.IntegrationServices;
using GreenTech.SkyCrop.Platform.Shared.Application.Model;
using GreenTech.SkyCrop.Platform.Shared.Domain.Model.Errors;

namespace GreenTech.SkyCrop.Platform.Diagnoses.Infrastructure.Integration;

/// <summary>
/// Prevents diagnosis access until the IAM/Plots integration is configured.
/// This is not a production authorization implementation.
/// </summary>
public sealed class UnconfiguredPlotAccessService : IPlotAccessService
{
    public Task<Result> CheckAccessAsync(
        int plotId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (plotId <= 0)
        {
            return Task.FromResult(Result.Failure(Error.Validation(
                "Diagnoses.InvalidPlotId",
                "A positive plot identifier is required.")));
        }

        return Task.FromResult(Result.Failure(MissingIntegration()));
    }

    public Task<Result<IReadOnlyCollection<int>>> GetAccessiblePlotIdsAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(
            Result<IReadOnlyCollection<int>>.Failure(MissingIntegration()));
    }

    private static Error MissingIntegration() => Error.Failure(
        "Diagnoses.AccessNotConfigured",
        "The integration with plot access permissions is not configured.");
}