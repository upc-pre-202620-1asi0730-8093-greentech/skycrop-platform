namespace GreenTech.SkyCrop.Platform.Reports.Domain.Model.ValueObjects;

public record ReportMetrics 
{
    public int Flights { get; }
    public int Alerts { get; }
    public float AverageNdvi { get; }
    public float AreaHectares { get; }
    
    /// <summary>
    /// Represents a collection of numbers related to a plot's performance
    /// </summary>
    /// <param name="flights">The number of drone flights done</param>
    /// <param name="alerts">The number of alerts received</param>
    /// <param name="averageNdvi">The average NDVI across all plots</param>
    /// <param name="areaHectares">The area of the plot in hectares</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public ReportMetrics(int flights, int alerts, float averageNdvi, float areaHectares)
    {
        if (flights < 0) throw new ArgumentOutOfRangeException(nameof(flights), @"Flights must be greater than or equal to 0.");

        if (alerts < 0) throw new ArgumentOutOfRangeException(nameof(alerts), @"Alerts must be greater than or equal to 0.");

        if (float.IsNaN(averageNdvi) || float.IsInfinity(averageNdvi) || averageNdvi < -1 || averageNdvi > 1)
            throw new ArgumentOutOfRangeException(nameof(averageNdvi), @"AverageNdvi must be between -1 and 1.");

        if (float.IsNaN(areaHectares) || float.IsInfinity(areaHectares) || areaHectares <= 0)
            throw new ArgumentOutOfRangeException(nameof(areaHectares), @"AreaHectares must be greater than or equal to 0.");
        
        Flights = flights;
        Alerts = alerts;
        AverageNdvi = averageNdvi;
        AreaHectares = areaHectares;
    }
}