using GreenTech.SkyCrop.Platform.Diagnoses.Application.IntegrationServices;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Entities;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.ValueObjects;

namespace GreenTech.SkyCrop.Platform.Diagnoses.Infrastructure.ImageAnalysis;

// Returns predefined results; it does not inspect image contents.
public sealed class SimulatedImageAnalysis : IImageAnalysis
{
    public Task<ImageAnalysisResult> AnalyzeAsync(
        ImageAnalysisInput input,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(input);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.Scenario);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.Location);

        if (input.Location.Trim().Length > 255)
            throw new ArgumentException(
                "Location must not exceed 255 characters.", nameof(input));

        ValidateCoordinates(input.Latitude, input.Longitude);

        var result = input.Scenario.Trim().ToUpperInvariant() switch
        {
            "HEALTHY" => new ImageAnalysisResult(
                "Simulated result: healthy crop. No real images were analyzed.",
                new Ndvi(0.8),
                Array.Empty<Anomaly>()),

            "WATER_STRESS" => CreateResult(
                input, "WATER_STRESS", "MEDIUM", 0.4,
                "Simulated water stress detected."),

            "PEST" => CreateResult(
                input, "PEST", "HIGH", 0.55,
                "Simulated pest presence detected."),

            _ => throw new ArgumentException(
                "Unknown simulation scenario.", nameof(input))
        };

        return Task.FromResult(result);
    }

    private static ImageAnalysisResult CreateResult(
        ImageAnalysisInput input,
        string type,
        string severity,
        double ndvi,
        string description)
    {
        var anomaly = new Anomaly(
            type,
            severity,
            description,
            input.Location.Trim(),
            input.Latitude,
            input.Longitude);

        return new ImageAnalysisResult(
            $"{description} No real images were analyzed.",
            new Ndvi(ndvi),
            Array.AsReadOnly(new[] { anomaly }));
    }

    private static void ValidateCoordinates(double? latitude, double? longitude)
    {
        if ((latitude is null) != (longitude is null))
            throw new ArgumentException(
                "Latitude and longitude must be supplied together.");

        if (latitude is double lat &&
            (!double.IsFinite(lat) || Math.Abs(lat) > 90))
            throw new ArgumentException(
                "Latitude must be between -90 and 90.");

        if (longitude is double lon &&
            (!double.IsFinite(lon) || Math.Abs(lon) > 180))
            throw new ArgumentException(
                "Longitude must be between -180 and 180.");
    }
}