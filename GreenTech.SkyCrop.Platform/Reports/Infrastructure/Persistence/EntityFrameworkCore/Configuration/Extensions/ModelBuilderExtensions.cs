using GreenTech.SkyCrop.Platform.Reports.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Reports.Domain.Model.ValueObjects;
using GreenTech.SkyCrop.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GreenTech.SkyCrop.Platform.Reports.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

public static class ModelBuilderExtensions
{
    /// <summary>
    /// Configures the data model for the report aggregate and its relations
    /// </summary>
    /// <param name="builder">The model builder provided by Entity Framework Core</param>
    public static void ApplyReportsConfiguration(this ModelBuilder builder)
    {
        var utcTime = new ValueConverter<DateTimeOffset, DateTime>(
            value => value.UtcDateTime,
            value => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)));

        var reports = builder.Entity<Report>();
        
        reports.ToTable("Reports");
        reports.HasKey(r => r.Id);
        reports.Property(p => p.Id)
            .HasConversion(new StronglyTypedIdConverter<ReportId, int>(i=> i.Value, v => new ReportId(v)))
            .IsRequired()
            .ValueGeneratedOnAdd();
        
        reports.Property(r => r.Title)
            .HasConversion(t => t.Value, v => new ReportTitle(v))
            .HasMaxLength(100)
            .IsRequired();

        reports.Property(r => r.GeneratedAt)
            .HasConversion(utcTime);

        reports.Property(r => r.Season)
            .HasConversion(s => s.ToString(), v => Enum.Parse<ReportSeason>(v))
            .HasMaxLength(20);
        
        reports.Property(r => r.Year);

        reports.Property(r => r.Owner)
            .HasConversion(o => o.Name, v => new PersonName(v))
            .HasMaxLength(150);
        
        reports.Property(r => r.GeneratedBy)
            .HasConversion(g => g.Name, v => new DroneName(v))
            .HasMaxLength(100);
        
        reports.Property(r => r.Type)
            .HasConversion(t => t.ToString(), v => Enum.Parse<ReportType>(v))
            .HasMaxLength(30);

        reports.OwnsOne(r => r.ReportMetrics, metrics =>
        {
            metrics.WithOwner().HasForeignKey("Id");
            metrics.Property(m => m.Flights).HasColumnName("Flights");
            metrics.Property(m => m.Alerts).HasColumnName("Alerts");
            metrics.Property(m => m.AverageNdvi).HasColumnName("AverageNdvi");
            metrics.Property(m => m.AreaHectares).HasColumnName("AreaHectares");
        });

        reports.OwnsMany(r => r.NdviEvolution, ndvi =>
        {
            ndvi.ToTable("NdviEvolutionItems");
            ndvi.WithOwner().HasForeignKey("ReportId");
            ndvi.HasKey("ReportId", "Date");
            ndvi.Property(n => n.Date).HasConversion(utcTime).HasColumnName("Date").IsRequired();
            ndvi.Property(n => n.Value).HasColumnName("Value").IsRequired();
        });

        reports.OwnsMany(r => r.Performance, performance =>
        {
            performance.ToTable("ReportPerformanceItems");
            performance.WithOwner().HasForeignKey("ReportId");
            performance.HasKey("ReportId", "PlotId");
            performance.Property(p => p.PlotId).HasColumnName("PlotId").ValueGeneratedNever().IsRequired();
            performance.Property(p => p.Alerts).HasColumnName("Alerts");
            performance.Property(p => p.Flights).HasColumnName("Flights");
            performance.Property(p => p.AverageNdvi).HasColumnName("AverageNdvi");
        });
    }
}