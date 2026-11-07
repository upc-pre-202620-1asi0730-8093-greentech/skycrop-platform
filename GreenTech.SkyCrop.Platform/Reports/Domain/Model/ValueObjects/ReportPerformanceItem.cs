namespace GreenTech.SkyCrop.Platform.Reports.Domain.Model.ValueObjects;

public record ReportPerformanceItem
{
   public int PlotId { get; }
   public string PlotName { get; }
   public List<string> Crops { get; }
   public int Alerts { get; }
   public int Flights { get; }
   public float AverageNdvi { get; }

   public ReportPerformanceItem(Dictionary<string, object> item)
   {
      
      if (!item.TryGetValue("plotId", out var plotId))
         throw new ArgumentException("PlotId not found");
      
      if (!item.TryGetValue("plotName", out var plotName))
         throw new ArgumentException("PlotName not found");
      
      if (!item.TryGetValue("Crops", out var crops))
         throw new ArgumentException("Crops not found");
      
      if (!item.TryGetValue("Alerts", out var alerts))
         throw new ArgumentException("Alerts not found");
      
      if (!item.TryGetValue("Flights", out var flights))
         throw new ArgumentException("Flights not found");
      
      if (!item.TryGetValue("AverageNdvi", out var averageNdvi))
         throw new ArgumentException("AverageNdvi not found");
      
      PlotId = Convert.ToInt32(plotId);
      PlotName = Convert.ToString(plotName)!;
      Crops = (crops as List<string>)!;
      Alerts = Convert.ToInt32(alerts);
      Flights = Convert.ToInt32(flights);
      AverageNdvi = Convert.ToSingle(averageNdvi);
      
      if (float.IsNaN(AverageNdvi) || float.IsInfinity(AverageNdvi) || AverageNdvi < -1 || AverageNdvi > 1)
         throw new ArgumentOutOfRangeException(nameof(AverageNdvi), @"AverageNdvi must be between -1 and 1.");
   }
   
   
   
};