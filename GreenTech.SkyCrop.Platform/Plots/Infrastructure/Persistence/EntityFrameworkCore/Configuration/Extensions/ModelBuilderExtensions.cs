using GreenTech.SkyCrop.Platform.Plots.Domain.Model.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace GreenTech.SkyCrop.Platform.Plots.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

internal static class ModelBuilderExtensions
{
    internal static void ApplyPlotsConfiguration(this ModelBuilder builder)
    {
        builder.Entity<AgriculturalPlot>(plot =>
        {
            plot.ToTable("agricultural_plots");
            plot.HasKey(entity => entity.Id);
            plot.Property(entity => entity.Name).HasMaxLength(AgriculturalPlot.TextLength);
            plot.Property(entity => entity.PersonInCharge).HasMaxLength(AgriculturalPlot.TextLength);
            plot.Property(entity => entity.CropType).HasMaxLength(AgriculturalPlot.TextLength);
            plot.Property(entity => entity.SoilType).HasMaxLength(AgriculturalPlot.TextLength);
            plot.Property(entity => entity.SowingDate)
                .HasConversion(date => date.ToDateTime(TimeOnly.MinValue), value => DateOnly.FromDateTime(value))
                .HasColumnType("date");
            plot.Property(entity => entity.AreaHectares).HasPrecision(18, 6);
            plot.Property(entity => entity.Location).HasMaxLength(AgriculturalPlot.LocationLength);
            plot.Property(entity => entity.ImageUrl).HasMaxLength(AgriculturalPlot.ImageUrlLength);
            plot.Property(entity => entity.Status).HasMaxLength(8);
        });
    }
}
