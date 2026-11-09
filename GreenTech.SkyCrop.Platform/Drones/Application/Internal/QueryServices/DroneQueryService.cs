using GreenTech.SkyCrop.Platform.Drones.Application.QueryServices;
using GreenTech.SkyCrop.Platform.Drones.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Drones.Domain.Model.Entities;
using GreenTech.SkyCrop.Platform.Drones.Domain.Model.Queries;
using GreenTech.SkyCrop.Platform.Drones.Domain.Repositories;

namespace GreenTech.SkyCrop.Platform.Drones.Application.Internal.QueryServices;

public sealed class DroneQueryService(IDroneRepository drones, IFlightRouteRepository routes,
    IFlightRepository flights, IAerialImageRepository images) : IDroneQueryService
{
    public Task<IEnumerable<Drone>> Handle(GetAllDronesQuery query, CancellationToken cancellationToken = default) => drones.ListAsNoTrackingAsync(cancellationToken);
    public Task<Drone?> Handle(GetDroneByIdQuery query, CancellationToken cancellationToken = default) => drones.FindByIdAsNoTrackingAsync(query.DroneId, cancellationToken);
    public Task<IEnumerable<FlightRoute>> Handle(GetAllFlightRoutesQuery query, CancellationToken cancellationToken = default) => routes.ListAsNoTrackingAsync(cancellationToken);
    public Task<FlightRoute?> Handle(GetFlightRouteByIdQuery query, CancellationToken cancellationToken = default) => routes.FindByIdAsNoTrackingAsync(query.RouteId, cancellationToken);
    public Task<IEnumerable<Flight>> Handle(GetAllFlightsQuery query, CancellationToken cancellationToken = default) => flights.ListAsNoTrackingAsync(cancellationToken);
    public Task<Flight?> Handle(GetFlightByIdQuery query, CancellationToken cancellationToken = default) => flights.FindByIdAsNoTrackingAsync(query.FlightId, cancellationToken);
    public Task<IEnumerable<AerialImage>> Handle(GetAllAerialImagesQuery query, CancellationToken cancellationToken = default) => images.ListAsNoTrackingAsync(cancellationToken);
    public Task<AerialImage?> Handle(GetAerialImageByIdQuery query, CancellationToken cancellationToken = default) => images.FindByIdAsNoTrackingAsync(query.ImageId, cancellationToken);
}
