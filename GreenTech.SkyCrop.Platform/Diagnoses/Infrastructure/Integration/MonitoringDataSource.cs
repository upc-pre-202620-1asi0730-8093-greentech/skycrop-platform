using GreenTech.SkyCrop.Platform.Diagnoses.Application.IntegrationServices;
using GreenTech.SkyCrop.Platform.Drones.Domain.Model;
using GreenTech.SkyCrop.Platform.Drones.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Drones.Domain.Model.Entities;
using GreenTech.SkyCrop.Platform.Plots.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Shared.Application.Model;
using GreenTech.SkyCrop.Platform.Shared.Domain.Model.Errors;
using GreenTech.SkyCrop.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Microsoft.EntityFrameworkCore;

namespace GreenTech.SkyCrop.Platform.Diagnoses.Infrastructure.Integration;

/// <summary>
/// Reads monitoring information from the shared database.
/// Does not modify Plots or Drones entities or grant access permissions.
/// </summary>
public sealed class MonitoringDataSource(AppDbContext context)
    : IMonitoringDataSource
{
    public async Task<Result<MonitoringData>> GetAsync(
        int plotId,
        int flightId,
        IReadOnlyList<int> imageIds,
        CancellationToken cancellationToken = default)
    {
        if (plotId <= 0 || flightId <= 0)
            return Invalid(
                "Diagnoses.InvalidReferences",
                "Positive plot and flight identifiers are required.");

        if (imageIds is null)
            return Invalid(
                "Diagnoses.InvalidImages",
                "Image identifiers are required.");

        var requestedImages = imageIds.ToArray();

        if (requestedImages.Length is < 1 or > 100 ||
            requestedImages.Any(id => id <= 0) ||
            requestedImages.Distinct().Count() != requestedImages.Length)
        {
            return Invalid(
                "Diagnoses.InvalidImages",
                "Provide between 1 and 100 unique positive image identifiers.");
        }

        var plot = await context.Set<AgriculturalPlot>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == plotId, cancellationToken);

        if (plot is null)
            return Result<MonitoringData>.Failure(Error.NotFound(
                "Diagnoses.PlotNotFound",
                "The plot was not found."));

        var flight = await context.Set<Flight>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == flightId && item.PlotId == plotId,
                cancellationToken);

        if (flight is null)
            return Result<MonitoringData>.Failure(Error.NotFound(
                "Diagnoses.FlightNotFound",
                "The flight was not found for this plot."));

        if (flight.Status != EFlightStatus.COMPLETED)
            return Result<MonitoringData>.Failure(Error.Conflict(
                "Diagnoses.FlightNotCompleted",
                "The flight must be completed before generating a diagnosis."));

        var images = await context.Set<AerialImage>()
            .AsNoTracking()
            .Where(item =>
                item.FlightId == flightId &&
                requestedImages.Contains(item.Id))
            .ToListAsync(cancellationToken);

        if (images.Count != requestedImages.Length)
            return Invalid(
                "Diagnoses.ImagesUnavailable",
                "Some images do not exist or do not belong to this flight.");

        var now = DateTimeOffset.UtcNow;

        if (images.Any(image =>
                image.CapturedAt == default || image.CapturedAt > now))
        {
            return Invalid(
                "Diagnoses.InvalidMonitoringDate",
                "Image capture dates must be valid and not in the future.");
        }

        // The latest capture marks the end of the selected monitoring period.
        var monitoredAt = images.Max(image => image.CapturedAt)
            .ToUniversalTime();

        return Result<MonitoringData>.Success(new MonitoringData(
            plot.Id,
            flight.Id,
            Array.AsReadOnly(requestedImages),
            monitoredAt,
            plot.Location,
            plot.Latitude,
            plot.Longitude));
    }

    private static Result<MonitoringData> Invalid(
        string code, string message) =>
        Result<MonitoringData>.Failure(Error.Validation(code, message));
}