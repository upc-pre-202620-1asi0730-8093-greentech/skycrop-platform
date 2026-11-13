using GreenTech.SkyCrop.Platform.Diagnoses.Application.IntegrationServices;
using GreenTech.SkyCrop.Platform.Shared.Application.Model;
using GreenTech.SkyCrop.Platform.Shared.Domain.Model.Errors;

// These implementations exist only in the test project.
// They are never registered in the API.
internal sealed class TestPlotAccessService(int allowedPlotId)
    : IPlotAccessService
{
    public Task<Result> CheckAccessAsync(
        int plotId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(
            plotId == allowedPlotId && allowedPlotId > 0
                ? Result.Success()
                : Result.Failure(Error.Forbidden(
                    "Diagnoses.PlotAccessDenied",
                    "Access to this plot is not allowed.")));
    }

    public Task<Result<IReadOnlyCollection<int>>> GetAccessiblePlotIdsAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyCollection<int> ids = allowedPlotId > 0
            ? new[] { allowedPlotId }
            : Array.Empty<int>();

        return Task.FromResult(
            Result<IReadOnlyCollection<int>>.Success(ids));
    }
}

internal sealed class FailingImageAnalysis : IImageAnalysis
{
    public Task<ImageAnalysisResult> AnalyzeAsync(
        ImageAnalysisInput input,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        throw new InvalidOperationException(
            "Internal test provider failure.");
    }
}