using GreenTech.SkyCrop.Platform.Drones.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Drones.Domain.Repositories;
using GreenTech.SkyCrop.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using GreenTech.SkyCrop.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GreenTech.SkyCrop.Platform.Drones.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public sealed class FlightRepository(AppDbContext context) : BaseRepository<Flight>(context), IFlightRepository
{
    public Task<bool> HasActiveFlightForDroneAsync(int droneId, CancellationToken cancellationToken = default) =>
        Context.Set<Flight>().AnyAsync(flight => flight.ActiveDroneId == droneId, cancellationToken);
    public Task<bool> HasActiveFlightForRouteAsync(int routeId, CancellationToken cancellationToken = default) =>
        Context.Set<Flight>().AnyAsync(flight => flight.FlightRouteId == routeId && flight.ActiveDroneId != null, cancellationToken);
}
