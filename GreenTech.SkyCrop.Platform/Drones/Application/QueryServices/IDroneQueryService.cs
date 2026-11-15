using GreenTech.SkyCrop.Platform.Drones.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Drones.Domain.Model.Entities;
using GreenTech.SkyCrop.Platform.Drones.Domain.Model.Queries;

namespace GreenTech.SkyCrop.Platform.Drones.Application.QueryServices;

public interface IDroneQueryService
{
    Task<IEnumerable<Drone>> Handle(GetAllDronesQuery query, CancellationToken cancellationToken = default);
    Task<Drone?> Handle(GetDroneByIdQuery query, CancellationToken cancellationToken = default);
    Task<IEnumerable<FlightRoute>> Handle(GetAllFlightRoutesQuery query, CancellationToken cancellationToken = default);
    Task<FlightRoute?> Handle(GetFlightRouteByIdQuery query, CancellationToken cancellationToken = default);
    Task<IEnumerable<Flight>> Handle(GetAllFlightsQuery query, CancellationToken cancellationToken = default);
    Task<Flight?> Handle(GetFlightByIdQuery query, CancellationToken cancellationToken = default);
    Task<IEnumerable<AerialImage>> Handle(GetAllAerialImagesQuery query, CancellationToken cancellationToken = default);
    Task<AerialImage?> Handle(GetAerialImageByIdQuery query, CancellationToken cancellationToken = default);
}
