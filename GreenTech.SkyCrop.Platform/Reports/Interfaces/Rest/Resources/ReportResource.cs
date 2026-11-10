namespace GreenTech.SkyCrop.Platform.Reports.Interfaces.Rest.Resources;

public record ReportResource(
    int Id,
    string Title,
    string GeneratedAt,
    string Season,
    int Year,
    string Owner,
    string GeneratedBy,
    string Type,
    ReportMetricsResource Metrics,
    List<NdviEvolutionItemResource> NdviEvolution,
    List<ReportPerformanceItemResource> Performance
);
