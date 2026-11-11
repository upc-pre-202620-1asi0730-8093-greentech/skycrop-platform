using GreenTech.SkyCrop.Platform.Drones.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Drones.Domain.Model.Entities;
using GreenTech.SkyCrop.Platform.Drones.Domain.Model.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MySql.EntityFrameworkCore.Extensions;

namespace GreenTech.SkyCrop.Platform.Drones.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

public static class ModelBuilderExtensions
{
    public static void ApplyDronesConfiguration(this ModelBuilder builder)
    {
        var utcTime = new ValueConverter<DateTimeOffset, DateTime>(
            value => value.UtcDateTime,
            value => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)));

        var drone = builder.Entity<Drone>();
        drone.ToTable("drones");
        drone.HasKey(d => d.Id);
        drone.Property(d => d.Name).HasMaxLength(120).IsRequired();
        drone.Property(d => d.Model).HasMaxLength(120).IsRequired();
        drone.Property(d => d.Camera).HasMaxLength(120).IsRequired();
        drone.Property(d => d.SerialNumber).HasConversion(value => value.Value, value => new DroneSerialNumber(value)).HasMaxLength(120).IsRequired();
        drone.Property(d => d.SerialNumberKey).HasMaxLength(120).IsRequired();
        drone.Property(d => d.SerialNumberKey).ForMySQLHasCollation("utf8mb4_bin");
        drone.HasIndex(d => d.SerialNumberKey).IsUnique().HasDatabaseName("ux_drones_serial_number_key");
        drone.Property(d => d.Battery).HasConversion(value => value.Value, value => new BatteryLevel(value)).HasPrecision(18, 6);
        drone.Property(d => d.AutonomyMinutes).HasPrecision(18, 6);
        drone.Property(d => d.Status).HasConversion<string>().HasMaxLength(20);
        drone.Property(d => d.ImageUrl).HasMaxLength(2048);
        drone.Property(d => d.Version).IsConcurrencyToken();
        drone.Property(d => d.CreatedAt).HasConversion(utcTime);
        drone.Property(d => d.UpdatedAt).HasConversion(utcTime);
        drone.Ignore(d => d.DomainEvents);

        var route = builder.Entity<FlightRoute>();
        route.ToTable("flight_routes");
        route.HasKey(r => r.Id);
        route.Property(r => r.Name).HasMaxLength(120).IsRequired();
        route.Property(r => r.Version).IsConcurrencyToken();
        route.Ignore(r => r.DomainEvents);
        route.OwnsOne(r => r.Parameters, parameters =>
        {
            parameters.WithOwner().HasForeignKey("Id");
            parameters.Property(p => p.AltitudeMeters).HasColumnName("altitude_meters").HasPrecision(18, 6);
            parameters.Property(p => p.SpeedMetersPerSecond).HasColumnName("speed_meters_per_second").HasPrecision(18, 6);
        });
        route.Navigation(r => r.Parameters).IsRequired();
        route.OwnsMany(r => r.Waypoints, points =>
        {
            points.ToTable("flight_route_waypoints");
            points.WithOwner().HasForeignKey("FlightRouteId");
            points.HasKey("FlightRouteId", nameof(RouteWaypoint.Sequence));
            points.Property(p => p.Sequence).ValueGeneratedNever();
            points.Property(p => p.Latitude).IsRequired();
            points.Property(p => p.Longitude).IsRequired();
        });
        route.Navigation(r => r.Waypoints).UsePropertyAccessMode(PropertyAccessMode.Field);

        var flight = builder.Entity<Flight>();
        flight.ToTable("flights");
        flight.HasKey(f => f.Id);
        flight.Property(f => f.Status).HasConversion<string>().HasMaxLength(20);
        flight.Property(f => f.Version).IsConcurrencyToken();
        flight.Property(f => f.CreatedAt).HasConversion(utcTime);
        flight.Property(f => f.UpdatedAt).HasConversion(utcTime);
        flight.Property(f => f.StartedAt).HasConversion(utcTime);
        flight.Property(f => f.DurationMinutes).HasPrecision(18, 6);
        flight.HasIndex(f => f.ActiveDroneId).IsUnique().HasDatabaseName("ux_flights_active_drone_id");
        flight.HasOne<Drone>().WithMany().HasForeignKey(f => f.DroneId).OnDelete(DeleteBehavior.Restrict);
        flight.HasOne<FlightRoute>().WithMany().HasForeignKey(f => f.FlightRouteId).OnDelete(DeleteBehavior.Restrict);
        flight.Ignore(f => f.DomainEvents);

        var image = builder.Entity<AerialImage>();
        image.ToTable("aerial_images");
        image.HasKey(i => i.Id);
        image.Property(i => i.FileUrl).HasMaxLength(2048).IsRequired();
        image.Property(i => i.CapturedAt).HasConversion(utcTime);
        image.HasOne<Flight>().WithMany().HasForeignKey(i => i.FlightId).OnDelete(DeleteBehavior.Restrict);
    }
}
