namespace GreenTech.SkyCrop.Platform.Reports.Domain.Model.ValueObjects;

/// <summary>
/// Represents the type of the report
/// </summary>
public enum ReportType
{
    /// <summary>
    /// The report was generated via drone monitoring
    /// </summary>
    Monitoring,
    
    /// <summary>
    /// The report was generated after the end of a season
    /// </summary>
    Seasonal
}