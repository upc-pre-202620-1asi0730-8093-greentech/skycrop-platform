namespace GreenTech.SkyCrop.Platform.Reports.Interfaces.Rest.Resources;

public record ReportPerformanceItemResource(
    int PlotId,
    int Alerts,
    int Flights,
    float AverageNdvi 
);
