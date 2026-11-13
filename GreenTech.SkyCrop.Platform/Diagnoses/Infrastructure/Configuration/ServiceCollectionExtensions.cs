using GreenTech.SkyCrop.Platform.Diagnoses.Application.IntegrationServices;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Repositories;
using GreenTech.SkyCrop.Platform.Diagnoses.Infrastructure.ImageAnalysis;
using GreenTech.SkyCrop.Platform.Diagnoses.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Microsoft.Extensions.DependencyInjection;
using GreenTech.SkyCrop.Platform.Diagnoses.Infrastructure.Integration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using GreenTech.SkyCrop.Platform.Diagnoses.Application.CommandServices;
using GreenTech.SkyCrop.Platform.Diagnoses.Application.Internal.CommandServices;
using GreenTech.SkyCrop.Platform.Shared.Domain.Repositories;
using GreenTech.SkyCrop.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace GreenTech.SkyCrop.Platform.Diagnoses.Infrastructure.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDiagnosesContext(
        this IServiceCollection services)
    {
        services.AddScoped<IDiagnosisRepository, DiagnosisRepository>();
        services.AddScoped<IImageAnalysis, SimulatedImageAnalysis>();
        services.AddScoped<IMonitoringDataSource, MonitoringDataSource>();
        services.TryAddScoped<IPlotAccessService, UnconfiguredPlotAccessService>();
        services.AddLogging();
        services.TryAddSingleton<TimeProvider>(TimeProvider.System);
        services.TryAddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IDiagnosisCommandService, DiagnosisCommandService>();
        return services;
    }
}