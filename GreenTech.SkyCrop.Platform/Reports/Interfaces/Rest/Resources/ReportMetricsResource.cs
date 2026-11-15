namespace GreenTech.SkyCrop.Platform.Reports.Interfaces.Rest.Resources;

/// <summary>
/// Value object resource
/// </summary>
public record ReportMetricsResource(
    int Flights,
    int Alerts,
    float AverageNdvi,
    float AreaHectares
    );