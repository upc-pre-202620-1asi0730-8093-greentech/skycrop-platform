namespace GreenTech.SkyCrop.Platform.Reports.Interfaces.Rest.Resources;

/// <summary>
/// Value object resource
/// </summary>
public record ReportPerformanceItemResource(
    int PlotId,
    int Alerts,
    int Flights,
    float AverageNdvi 
);
