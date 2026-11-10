namespace GreenTech.SkyCrop.Platform.Reports.Domain.Model.ValueObjects;

public record ReportPerformanceItem
{
   public int PlotId { get; }
   public int Alerts { get; }
   public int Flights { get; }
   public float AverageNdvi { get; }
   public ReportPerformanceItem(int plotId, int alerts, int flights, float averageNdvi)
   {
      
      if (float.IsNaN(AverageNdvi) || float.IsInfinity(AverageNdvi) || AverageNdvi < -1 || AverageNdvi > 1)
         throw new ArgumentOutOfRangeException(nameof(AverageNdvi), @"AverageNdvi must be between -1 and 1.");
      
      PlotId = plotId;
      Alerts = alerts;
      Flights = flights;
      AverageNdvi = averageNdvi;
      

   }
   
   public static ReportPerformanceItem FromDictionary(Dictionary<string, object> item)
   {
      if (!item.TryGetValue("plotId", out var plotId) && !item.TryGetValue("plotId", out plotId))
         throw new ArgumentException("PlotId not found");
      
      if (!item.TryGetValue("Alerts", out var alerts) && !item.TryGetValue("alerts", out alerts))
         throw new ArgumentException("Alerts not found");
      
      if (!item.TryGetValue("Flights", out var flights) && !item.TryGetValue("flights", out flights))
         throw new ArgumentException("Flights not found");
      
      if (!item.TryGetValue("AverageNdvi", out var averageNdvi) && !item.TryGetValue("averageNdvi", out averageNdvi))
         throw new ArgumentException("AverageNdvi not found");
      
      var pId = Convert.ToInt32(plotId);
      var pAlerts = Convert.ToInt32(alerts);
      var pFlights = Convert.ToInt32(flights);
      var pAvgNdvi = Convert.ToSingle(averageNdvi);
      
      return new ReportPerformanceItem(pId, pAlerts, pFlights, pAvgNdvi);
   }
}