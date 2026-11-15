namespace GreenTech.SkyCrop.Platform.Diagnoses.Application.IntegrationServices;

/// <summary>
/// Verified monitoring references and plot location for diagnosis generation.
/// Does not represent authorization to access the plot.
/// </summary>
public sealed record MonitoringData(
    int PlotId,
    int FlightId,
    IReadOnlyList<int> ImageIds,
    DateTimeOffset MonitoredAt,
    string Location,
    double? Latitude,
    double? Longitude);