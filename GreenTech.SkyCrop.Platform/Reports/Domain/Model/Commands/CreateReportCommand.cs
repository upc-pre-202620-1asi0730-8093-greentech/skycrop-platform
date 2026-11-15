namespace GreenTech.SkyCrop.Platform.Reports.Domain.Model.Commands;

/// <summary>
/// Create report command
/// </summary>
/// <param name="Title">The title of the report</param>
/// <param name="GeneratedAt">The date of generation of the report</param>
/// <param name="Season">The target season of the report</param>
/// <param name="Year">The year of generation of the report</param>
/// <param name="Owner">The name of the user owner of the report</param>
/// <param name="GeneratedBy">The name of the system that generated the report</param>
/// <param name="Type">The type of the report</param>
/// <param name="Flights">The number of flights recorded on the report</param>
/// <param name="Alerts">The number of alerts recorded on the report</param>
/// <param name="AverageNdvi">The average NDVI across all plots</param>
/// <param name="AreaHectares">The area of the plot in hectares</param>
/// <param name="NdviEvolution">Date and value history of the average NDVI</param>
/// <param name="Performance">The performance of each plot</param>
public record CreateReportCommand(
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
    List<Dictionary<string, object>> NdviEvolution,
    List<Dictionary<string, object>> Performance
    
);

