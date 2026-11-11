namespace GreenTech.SkyCrop.Platform.Reports.Interfaces.Rest.Resources;

/// <summary>
/// Resource used to create a report
/// </summary>
public record CreateReportResource(
    string Title,
    string GeneratedAt,
    string Season,
    int Year,
    string Owner,
    string GeneratedBy,
    string Type,
    int Flights,
    int Alerts,
    float AverageNdvi,
    float AreaHectares,
    List<NdviEvolutionItemResource> NdviEvolution,
    List<ReportPerformanceItemResource> Performance
    );
    