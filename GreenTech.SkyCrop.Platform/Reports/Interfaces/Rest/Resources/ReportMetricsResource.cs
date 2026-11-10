namespace GreenTech.SkyCrop.Platform.Reports.Interfaces.Rest.Resources;

public record ReportMetricsResource(
    int Flights,
    int Alerts,
    float AverageNdvi,
    float AreaHectares
    );