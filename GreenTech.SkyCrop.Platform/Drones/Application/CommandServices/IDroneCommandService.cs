using GreenTech.SkyCrop.Platform.Drones.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Drones.Domain.Model.Commands;
using GreenTech.SkyCrop.Platform.Shared.Application.Model;

namespace GreenTech.SkyCrop.Platform.Drones.Application.CommandServices;

public interface IDroneCommandService
{
    Task<Result<Drone>> Handle(CreateDroneCommand command, CancellationToken cancellationToken = default);
    Task<Result<Drone>> Handle(UpdateDroneCommand command, CancellationToken cancellationToken = default);
    Task<Result<FlightRoute>> Handle(CreateFlightRouteCommand command, CancellationToken cancellationToken = default);
    Task<Result<FlightRoute>> Handle(UpdateFlightRouteCommand command, CancellationToken cancellationToken = default);
    Task<Result<Flight>> Handle(PlanFlightCommand command, CancellationToken cancellationToken = default);
    Task<Result<Flight>> Handle(CancelFlightCommand command, CancellationToken cancellationToken = default);
}
