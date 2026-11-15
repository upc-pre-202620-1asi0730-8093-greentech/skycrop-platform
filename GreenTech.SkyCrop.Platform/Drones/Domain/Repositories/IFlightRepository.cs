using GreenTech.SkyCrop.Platform.Drones.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Shared.Domain.Repositories;

namespace GreenTech.SkyCrop.Platform.Drones.Domain.Repositories;

public interface IFlightRepository : IBaseRepository<Flight>
{
    Task<bool> HasActiveFlightForDroneAsync(int droneId, CancellationToken cancellationToken = default);
    Task<bool> HasActiveFlightForRouteAsync(int routeId, CancellationToken cancellationToken = default);
}
