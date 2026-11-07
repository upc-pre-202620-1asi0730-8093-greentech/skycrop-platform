using GreenTech.SkyCrop.Platform.Reports.Domain.Model.Commands;
using GreenTech.SkyCrop.Platform.Reports.Domain.Model.Events;
using GreenTech.SkyCrop.Platform.Reports.Domain.Model.ValueObjects;
using GreenTech.SkyCrop.Platform.Shared.Domain.Model.Aggregates;

namespace GreenTech.SkyCrop.Platform.Reports.Domain.Model.Aggregates;

public partial class Report : AggregateRoot
{
    public ReportId Id { get; }
    public ReportTitle Title { get; private set; }
    public DateTimeOffset  GeneratedAt { get; private set; }
    public ReportSeason Season { get; private set; }
    public int Year { get; private set; }
    public PersonName Owner { get; private set; }
    public DroneName GeneratedBy { get; private set; }
    public ReportType Type { get; private set; }
    public ReportMetrics ReportMetrics { get; private set; }
    public NdviEvolutionId NdviEvolutionId { get; private set; }
    public PerformanceId PerformanceId { get; private set; }
    
    public Report(CreateReportCommand command)
    {
        Title = new ReportTitle(command.Title);
        GeneratedAt = DateTimeOffset.Parse(command.GeneratedAt);
        if (Enum.TryParse<ReportSeason>(command.Season, true, out var season)) Season = season;
        Year = command.Year;
        Owner = new PersonName(command.Owner);
        GeneratedBy = new DroneName(command.GeneratedBy);
        if (Enum.TryParse<ReportType>(command.Type, true, out var type)) Type = type;
        ReportMetrics = new ReportMetrics(command.Flights, command.Alerts, command.AverageNdvi, command.AreaHectares);
        NdviEvolutionId = new NdviEvolutionId(command.NdviEvolutionId);
        PerformanceId = new PerformanceId(command.PerformanceId);
        
        AddDomainEvent(() => new ReportCreatedEvent(Id));
    }
}