using GreenTech.SkyCrop.Platform.Plots.Application.Acl;
using GreenTech.SkyCrop.Platform.Plots.Interfaces.Acl;
using GreenTech.SkyCrop.Platform.Plots.Application.CommandServices;
using GreenTech.SkyCrop.Platform.Plots.Application.Internal.CommandServices;
using GreenTech.SkyCrop.Platform.Plots.Application.Internal.QueryServices;
using GreenTech.SkyCrop.Platform.Plots.Application.QueryServices;
using GreenTech.SkyCrop.Platform.Plots.Domain.Repositories;
using GreenTech.SkyCrop.Platform.Plots.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace GreenTech.SkyCrop.Platform.Plots.Infrastructure.Configuration;

internal static class ServiceCollectionExtensions
{
    internal static IServiceCollection AddPlotsContext(this IServiceCollection services)
    {
        services.AddScoped<IAgriculturalPlotRepository, AgriculturalPlotRepository>();
        services.AddScoped<IAgriculturalPlotCommandService, AgriculturalPlotCommandService>();
        services.AddScoped<IAgriculturalPlotQueryService, AgriculturalPlotQueryService>();
        services.AddScoped<IPlotsContextFacade, PlotsContextFacade>();
        return services;
    }
}
