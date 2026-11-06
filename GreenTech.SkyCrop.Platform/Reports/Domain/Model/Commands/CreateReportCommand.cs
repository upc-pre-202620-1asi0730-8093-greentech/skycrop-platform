namespace GreenTech.SkyCrop.Platform.Reports.Domain.Model.Commands;

public record CreateReportCommand(
    string Title,
    string GeneratedAt,
    string Season,
    int Year,
    string Owner,
    string GeneratedBy,
    string Type,
    int MetricsId,
    int NdviEvolutionId,
    int PerformanceId
);

