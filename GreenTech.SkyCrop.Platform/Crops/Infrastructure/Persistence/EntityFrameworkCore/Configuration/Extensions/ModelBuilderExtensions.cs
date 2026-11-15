using GreenTech.SkyCrop.Platform.Crops.Domain.Model.ValueObjects;
using GreenTech.SkyCrop.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration.ValueConverters;
using Microsoft.EntityFrameworkCore;

namespace GreenTech.SkyCrop.Platform.Crops.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

public static class ModelBuilderExtensions
{
    internal static void ApplyCropsConfiguration(this ModelBuilder builder)
    {
        builder.Entity<Domain.Model.Aggregates.Crop>(crop =>
        {
            crop.ToTable("crops");
            crop.HasKey(entity => entity.Id);
            crop.Property(entity => entity.Id)
                .HasConversion(new StronglyTypedIdConverter<CropId, int>(id => id.Value, value => new CropId(value)))
                .IsRequired()
                .ValueGeneratedOnAdd();
            crop.Property(entity => entity.Name).HasMaxLength(Domain.Model.Aggregates.Crop.TextLength);
            crop.Property(entity=> entity.Variety).HasMaxLength(Domain.Model.Aggregates.Crop.TextLength);
            crop.Property(entity => entity.PlantingDate)
                .HasConversion(date => date.ToDateTime(TimeOnly.MinValue), value => DateOnly.FromDateTime(value))
                .HasColumnType("date");
            crop.Property(entity => entity.ExpectedHarvest)
                .HasConversion(date => date.ToDateTime(TimeOnly.MinValue), value => DateOnly.FromDateTime(value))
                .HasColumnType("date");
            crop.Property(entity => entity.PlantedAreaHectares).HasPrecision(18, Domain.Model.Aggregates.Crop.DecimalMax);
            crop.Property(entity => entity.SowingMethod).HasMaxLength(Domain.Model.Aggregates.Crop.TextLength);
            crop.Property(entity => entity.IrrigationType).HasMaxLength(Domain.Model.Aggregates.Crop.TextLength);
            crop.Property(entity => entity.SoilType).HasMaxLength(Domain.Model.Aggregates.Crop.TextLength);
            crop.Property(entity => entity.Notes).HasMaxLength(Domain.Model.Aggregates.Crop.TextLength);
            crop.Property(entity => entity.ImageURl).HasMaxLength(Domain.Model.Aggregates.Crop.UrlLength);
            crop.Property(entity => entity.GrowthStage).HasMaxLength(Domain.Model.Aggregates.Crop.TextLength);
            crop.Property(entity => entity.IdealHumidityMin).HasPrecision(5, Domain.Model.Aggregates.Crop.DecimalMax);
            crop.Property(entity => entity.IdealHumidityMax).HasPrecision(5, Domain.Model.Aggregates.Crop.DecimalMax);
        });
    }
}