using GreenTech.SkyCrop.Platform.Crops.Application.CommandServices;
using GreenTech.SkyCrop.Platform.Crops.Application.Internal.CommandServices;
using GreenTech.SkyCrop.Platform.Crops.Application.Internal.QueryServices;
using GreenTech.SkyCrop.Platform.Crops.Application.QueryServices;
using GreenTech.SkyCrop.Platform.Crops.Domain.Repositories;
using GreenTech.SkyCrop.Platform.Crops.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace GreenTech.SkyCrop.Platform.Crops.Infrastructure.Configuration;

public static class ServiceCollectionExtensions
{
    internal static IServiceCollection AddCropsContext(this IServiceCollection services)
    {
        services.AddScoped<ICropRepository, CropRepository>();
        services.AddScoped<ICropCommandService, CropCommandService>();
        services.AddScoped<ICropQueryService, CropQueryService>();
        return services;
    }
}