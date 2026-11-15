using GreenTech.SkyCrop.Platform.Drones.Application.CommandServices;
using GreenTech.SkyCrop.Platform.Drones.Domain.Model;
using GreenTech.SkyCrop.Platform.Drones.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Drones.Domain.Model.Commands;
using GreenTech.SkyCrop.Platform.Drones.Domain.Model.Errors;
using GreenTech.SkyCrop.Platform.Drones.Domain.Model.ValueObjects;
using GreenTech.SkyCrop.Platform.Drones.Domain.Repositories;
using GreenTech.SkyCrop.Platform.Shared.Application.Model;
using GreenTech.SkyCrop.Platform.Shared.Domain.Repositories;

namespace GreenTech.SkyCrop.Platform.Drones.Application.Internal.CommandServices;

public sealed class DroneCommandService(IDroneRepository drones, IFlightRouteRepository routes,
    IFlightRepository flights, IDroneUnitOfWork unitOfWork) : IDroneCommandService
{
    public Task<Result<Drone>> Handle(CreateDroneCommand command, CancellationToken cancellationToken = default) =>
        Execute(async () =>
        {
            var drone = new Drone(command);
            if (await drones.FindBySerialKeyAsync(drone.SerialNumber.Key, cancellationToken) is not null)
                return Result<Drone>.Failure(DroneErrors.DuplicateSerial);
            await drones.AddAsync(drone, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);
            return Result<Drone>.Success(drone);
        });

    public Task<Result<Drone>> Handle(UpdateDroneCommand command, CancellationToken cancellationToken = default) =>
        Execute(async () =>
        {
            var drone = await drones.FindByIdAsync(command.DroneId, cancellationToken);
            if (drone is null) return Result<Drone>.Failure(DroneErrors.NotFound("Drone"));
            if (await flights.HasActiveFlightForDroneAsync(drone.Id, cancellationToken))
                return Result<Drone>.Failure(DroneErrors.ActiveFlight);
            if (command.SerialNumber is not null)
            {
                var key = new DroneSerialNumber(command.SerialNumber).Key;
                var existing = await drones.FindBySerialKeyAsync(key, cancellationToken);
                if (existing is not null && existing.Id != drone.Id)
                    return Result<Drone>.Failure(DroneErrors.DuplicateSerial);
            }
            if (command.Name is null && command.Model is null && command.SerialNumber is null &&
                command.Camera is null && command.AutonomyMinutes is null && command.Battery is null &&
                command.ImageUrl is null && command.Status is null)
                return Result<Drone>.Failure(DroneErrors.InvalidData("At least one field must be supplied."));
            drone.Update(command);
            await unitOfWork.CompleteAsync(cancellationToken);
            return Result<Drone>.Success(drone);
        });

    public Task<Result<FlightRoute>> Handle(CreateFlightRouteCommand command, CancellationToken cancellationToken = default) =>
        Execute(async () =>
        {
            var route = new FlightRoute(command);
            await routes.AddAsync(route, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);
            return Result<FlightRoute>.Success(route);
        });

    public Task<Result<FlightRoute>> Handle(UpdateFlightRouteCommand command, CancellationToken cancellationToken = default) =>
        Execute(async () =>
        {
            var route = await routes.FindByIdAsync(command.RouteId, cancellationToken);
            if (route is null) return Result<FlightRoute>.Failure(DroneErrors.NotFound("FlightRoute"));
            if (await flights.HasActiveFlightForRouteAsync(route.Id, cancellationToken))
                return Result<FlightRoute>.Failure(DroneErrors.ActiveFlight);
            if (command.Name is null && command.PlotId is null && command.AltitudeMeters is null &&
                command.SpeedMetersPerSecond is null && command.Waypoints is null)
                return Result<FlightRoute>.Failure(DroneErrors.InvalidData("At least one field must be supplied."));
            route.Update(command);
            await unitOfWork.CompleteAsync(cancellationToken);
            return Result<FlightRoute>.Success(route);
        });

    public Task<Result<Flight>> Handle(PlanFlightCommand command, CancellationToken cancellationToken = default) =>
        Execute(async () =>
        {
            if (command.Status != nameof(EFlightStatus.PLANNED))
                return Result<Flight>.Failure(DroneErrors.InvalidData("New flights must have status PLANNED."));
            var drone = await drones.FindByIdAsync(command.DroneId, cancellationToken);
            if (drone is null) return Result<Flight>.Failure(DroneErrors.NotFound("Drone"));
            var route = await routes.FindByIdAsync(command.FlightRouteId, cancellationToken);
            if (route is null) return Result<Flight>.Failure(DroneErrors.NotFound("FlightRoute"));
            if (await flights.HasActiveFlightForDroneAsync(drone.Id, cancellationToken))
                return Result<Flight>.Failure(DroneErrors.ActiveFlight);
            var flight = new Flight(drone, route, command.PlotId);
            await flights.AddAsync(flight, cancellationToken);
            // The flight and the drone/route concurrency tokens are persisted atomically.
            await unitOfWork.CompleteAsync(cancellationToken);
            return Result<Flight>.Success(flight);
        });

    public Task<Result<Flight>> Handle(CancelFlightCommand command, CancellationToken cancellationToken = default) =>
        Execute(async () =>
        {
            if (command.Status != nameof(EFlightStatus.CANCELLED))
                return Result<Flight>.Failure(DroneErrors.InvalidData("This endpoint only accepts status CANCELLED."));
            var flight = await flights.FindByIdAsync(command.FlightId, cancellationToken);
            if (flight is null) return Result<Flight>.Failure(DroneErrors.NotFound("Flight"));
            flight.Cancel();
            await unitOfWork.CompleteAsync(cancellationToken);
            return Result<Flight>.Success(flight);
        });

    private static async Task<Result<T>> Execute<T>(Func<Task<Result<T>>> action)
    {
        try { return await action(); }
        catch (DronePersistenceConflictException exception) { return Result<T>.Failure(exception.Error); }
        catch (ArgumentException exception) { return Result<T>.Failure(DroneErrors.InvalidData(exception.Message)); }
        catch (DroneStateException exception)
        {
            return Result<T>.Failure(DroneErrors.InvalidOperation(exception.Message));
        }
    }
}
