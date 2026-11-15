using GreenTech.SkyCrop.Platform.Drones.Application.CommandServices;
using GreenTech.SkyCrop.Platform.Drones.Application.Internal.CommandServices;
using GreenTech.SkyCrop.Platform.Drones.Application.Internal.QueryServices;
using GreenTech.SkyCrop.Platform.Drones.Application.QueryServices;
using GreenTech.SkyCrop.Platform.Drones.Domain.Repositories;
using GreenTech.SkyCrop.Platform.Drones.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace GreenTech.SkyCrop.Platform.Drones.Infrastructure.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDronesContext(this IServiceCollection services)
    {
        services.AddScoped<IDroneRepository, DroneRepository>();
        services.AddScoped<IFlightRouteRepository, FlightRouteRepository>();
        services.AddScoped<IFlightRepository, FlightRepository>();
        services.AddScoped<IAerialImageRepository, AerialImageRepository>();
        services.AddScoped<IDroneUnitOfWork, DroneUnitOfWork>();
        services.AddScoped<IDroneCommandService, DroneCommandService>();
        services.AddScoped<IDroneQueryService, DroneQueryService>();
        return services;
    }
}
