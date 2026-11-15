using System.Text.Json;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.ValueObjects;
using GreenTech.SkyCrop.Platform.Drones.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Plots.Domain.Model.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GreenTech.SkyCrop.Platform.Diagnoses.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

public static class ModelBuilderExtensions
{
    public static void ApplyDiagnosesConfiguration(this ModelBuilder builder)
    {
        var utcTime = new ValueConverter<DateTimeOffset, DateTime>(
            value => value.UtcDateTime,
            value => new DateTimeOffset(
                DateTime.SpecifyKind(value, DateTimeKind.Utc)));

        var imageIdsConverter = new ValueConverter<List<int>, string>(
            value => JsonSerializer.Serialize(
                value, (JsonSerializerOptions?)null),
            value => JsonSerializer.Deserialize<List<int>>(
                value, (JsonSerializerOptions?)null)!);

        var imageIdsComparer = new ValueComparer<List<int>>(
            (left, right) => left!.SequenceEqual(right!),
            values => values.Aggregate(
                0, (hash, value) => HashCode.Combine(hash, value)),
            values => values.ToList());

        var diagnosis = builder.Entity<Diagnosis>();

        diagnosis.ToTable("diagnoses");
        diagnosis.HasKey(entity => entity.Id);

        diagnosis.Property(entity => entity.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        diagnosis.Property(entity => entity.Scenario)
            .HasMaxLength(30)
            .IsRequired();

        diagnosis.Property(entity => entity.Source)
            .HasMaxLength(20)
            .IsRequired();

        diagnosis.Property(entity => entity.Observations)
            .HasMaxLength(2000)
            .IsRequired();

        diagnosis.Property(entity => entity.FailureReason)
            .HasMaxLength(1000);

        diagnosis.Property(entity => entity.AverageNdvi)
            .HasConversion(
                value => value!.Value,
                value => new Ndvi(value))
            .IsRequired(false);

        diagnosis.Property(entity => entity.MonitoredAt)
            .HasConversion(utcTime);

        diagnosis.Property(entity => entity.RequestedAt)
            .HasConversion(utcTime);

        diagnosis.Property(entity => entity.GeneratedAt)
            .HasConversion(utcTime);

        diagnosis.Property(entity => entity.CreatedAt)
            .HasConversion(utcTime);

        diagnosis.Property(entity => entity.UpdatedAt)
            .HasConversion(utcTime);

        diagnosis.Ignore(entity => entity.DomainEvents);

        // Preserve the image references inside the aggregate.
        // Image existence and flight membership are checked by the application.
        diagnosis.Ignore(entity => entity.ImageIds);

        var imageIds = diagnosis.Property<List<int>>("_imageIds")
            .HasField("_imageIds")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("image_ids")
            .HasColumnType("longtext")
            .HasConversion(imageIdsConverter)
            .IsRequired();

        imageIds.Metadata.SetValueComparer(imageIdsComparer);

        diagnosis.HasIndex(entity => new
        {
            entity.PlotId,
            entity.MonitoredAt,
            entity.Id
        });

        diagnosis.HasOne<AgriculturalPlot>()
            .WithMany()
            .HasForeignKey(entity => entity.PlotId)
            .OnDelete(DeleteBehavior.Restrict);

        diagnosis.HasOne<Flight>()
            .WithMany()
            .HasForeignKey(entity => entity.FlightId)
            .OnDelete(DeleteBehavior.Restrict);

        diagnosis.OwnsMany(entity => entity.Anomalies, anomaly =>
        {
            anomaly.ToTable("diagnosis_anomalies");

            anomaly.WithOwner()
                .HasForeignKey(entity => entity.DiagnosisId);

            anomaly.HasKey(entity => entity.Id);

            anomaly.Property(entity => entity.Id)
                .ValueGeneratedOnAdd();

            anomaly.Property(entity => entity.Type)
                .HasMaxLength(40)
                .IsRequired();

            anomaly.Property(entity => entity.Severity)
                .HasMaxLength(20)
                .IsRequired();

            anomaly.Property(entity => entity.Description)
                .HasMaxLength(1000)
                .IsRequired();

            anomaly.Property(entity => entity.Location)
                .HasMaxLength(255)
                .IsRequired();

            anomaly.Property(entity => entity.Latitude)
                .IsRequired(false);

            anomaly.Property(entity => entity.Longitude)
                .IsRequired(false);
        });

        diagnosis.Navigation(entity => entity.Anomalies)
            .HasField("_anomalies")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}