using GreenTech.SkyCrop.Platform.Diagnoses.Application.IntegrationServices;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Repositories;
using GreenTech.SkyCrop.Platform.Diagnoses.Infrastructure.ImageAnalysis;
using GreenTech.SkyCrop.Platform.Diagnoses.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Microsoft.Extensions.DependencyInjection;
using GreenTech.SkyCrop.Platform.Diagnoses.Infrastructure.Integration;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
        return services;
    }
}