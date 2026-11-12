namespace GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Commands;

/// <summary>
/// Requests simulated analysis of images belonging to a plot's flight.
/// Results and lifecycle fields are calculated by the backend.
/// </summary>
public sealed record GenerateDiagnosisCommand(
    int PlotId,
    int FlightId,
    IReadOnlyList<int> ImageIds,
    string Scenario);