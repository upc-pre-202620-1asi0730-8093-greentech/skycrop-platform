namespace GreenTech.SkyCrop.Platform.Diagnoses.Application.IntegrationServices;

/// <summary>
/// Simulation inputs prepared by the application after validating
/// the plot, flight and image references.
/// </summary>
public sealed record ImageAnalysisInput(
    string Scenario,
    string Location,
    double? Latitude,
    double? Longitude);