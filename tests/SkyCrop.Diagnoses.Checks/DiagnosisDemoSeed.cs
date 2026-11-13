using System.Text.Json;
using GreenTech.SkyCrop.Platform.Drones.Domain.Model;
using GreenTech.SkyCrop.Platform.Drones.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Drones.Domain.Model.Commands;
using GreenTech.SkyCrop.Platform.Drones.Domain.Model.Entities;
using GreenTech.SkyCrop.Platform.Plots.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Plots.Domain.Model.Commands;
using GreenTech.SkyCrop.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Microsoft.EntityFrameworkCore;

internal static class DiagnosisDemoSeed
{
    public static async Task RunAsync()
    {
        var connection = Environment.GetEnvironmentVariable(
            "SKYCROP_TEST_CONNECTION");

        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException(
                "Configure SKYCROP_TEST_CONNECTION.");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(connection)
            .Options;

        await using var context = new AppDbContext(options);

        if (context.Database.GetDbConnection().Database
            != "skycrop_diagnoses_checks")
        {
            throw new InvalidOperationException(
                "Demo data requires skycrop_diagnoses_checks.");
        }

        if ((await context.Database.GetPendingMigrationsAsync()).Any())
            throw new InvalidOperationException(
                "Apply pending migrations before creating demo data.");

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var plot = new AgriculturalPlot(new RegisterPlotCommand(
            "Diagnoses local demo",
            "Demo operator",
            "Potato",
            "Loam",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
            1m,
            "Demo north sector",
            -12.0464,
            -77.0428,
            null));

        context.Add(plot);
        await context.SaveChangesAsync();

        var drone = new Drone(new CreateDroneCommand(
            "Diagnosis demo drone",
            "Demo model",
            $"demo-{Guid.NewGuid():N}",
            "Demo camera",
            30,
            100));

        context.Add(drone);
        await context.SaveChangesAsync();

        drone.Update(new UpdateDroneCommand(
            drone.Id, Status: "AVAILABLE"));

        var route = new FlightRoute(new CreateFlightRouteCommand(
            "Diagnosis demo route",
            plot.Id,
            30,
            4,
            new[]
            {
                new RouteWaypoint(0, -12.0464, -77.0428),
                new RouteWaypoint(1, -12.0465, -77.0429)
            }));

        context.Add(route);
        await context.SaveChangesAsync();

        var flight = new Flight(drone, route, plot.Id);
        context.Add(flight);
        await context.SaveChangesAsync();

        // Demo fixture only. This does not execute or complete a real flight.
        context.Entry(flight)
            .Property(item => item.Status)
            .CurrentValue = EFlightStatus.COMPLETED;

        context.Entry(flight)
            .Property(item => item.ActiveDroneId)
            .CurrentValue = null;

        var image = new AerialImage(
            flight.Id,
            "https://example.com/diagnoses-demo.jpg",
            DateTimeOffset.UtcNow.AddMinutes(-10));

        context.Add(image);
        await context.SaveChangesAsync();

        await transaction.CommitAsync();

        Console.WriteLine("Demo fixtures committed.");
        Console.WriteLine($"Diagnoses__DemoPlotId = {plot.Id}");
        Console.WriteLine("Use this POST body:");

        Console.WriteLine(JsonSerializer.Serialize(new
        {
            plotId = plot.Id,
            flightId = flight.Id,
            imageIds = new[] { image.Id },
            scenario = "WATER_STRESS"
        }, new JsonSerializerOptions { WriteIndented = true }));
    }
}