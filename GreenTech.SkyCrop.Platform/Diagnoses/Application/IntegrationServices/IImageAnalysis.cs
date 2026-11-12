namespace GreenTech.SkyCrop.Platform.Diagnoses.Application.IntegrationServices;

/// <summary>
/// Provides analysis results without handling persistence.
/// </summary>
public interface IImageAnalysis
{
    Task<ImageAnalysisResult> AnalyzeAsync(
        ImageAnalysisInput input,
        CancellationToken cancellationToken = default);
}