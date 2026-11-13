using GreenTech.SkyCrop.Platform.Diagnoses.Application.Acl;
using GreenTech.SkyCrop.Platform.Diagnoses.Application.CommandServices;
using GreenTech.SkyCrop.Platform.Diagnoses.Application.IntegrationServices;
using GreenTech.SkyCrop.Platform.Diagnoses.Application.Internal.CommandServices;
using GreenTech.SkyCrop.Platform.Diagnoses.Application.Internal.QueryServices;
using GreenTech.SkyCrop.Platform.Diagnoses.Application.QueryServices;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Repositories;
using GreenTech.SkyCrop.Platform.Diagnoses.Infrastructure.ImageAnalysis;
using GreenTech.SkyCrop.Platform.Diagnoses.Infrastructure.Integration;
using GreenTech.SkyCrop.Platform.Diagnoses.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using GreenTech.SkyCrop.Platform.Diagnoses.Interfaces.Acl;
using GreenTech.SkyCrop.Platform.Shared.Domain.Repositories;
using GreenTech.SkyCrop.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using MySql.Data.MySqlClient;

namespace GreenTech.SkyCrop.Platform.Diagnoses.Infrastructure.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDiagnosesContext(
        this IServiceCollection services)
    {
        services.AddScoped<IDiagnosisRepository, DiagnosisRepository>();
        services.AddScoped<IImageAnalysis, SimulatedImageAnalysis>();
        services.AddScoped<IMonitoringDataSource, MonitoringDataSource>();
        services.AddScoped<IDiagnosisCommandService, DiagnosisCommandService>();
        services.AddScoped<IDiagnosisQueryService, DiagnosisQueryService>();
        services.AddScoped<IDiagnosesContextFacade, DiagnosesContextFacade>();

        services.TryAddScoped<IPlotAccessService, UnconfiguredPlotAccessService>();
        services.TryAddScoped<IUnitOfWork, UnitOfWork>();
        services.TryAddSingleton<TimeProvider>(TimeProvider.System);
        services.AddLogging();

        return services;
    }

    public static IServiceCollection AddDiagnosesLocalDemo(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        if (!configuration.GetValue<bool>("Diagnoses:LocalDemoEnabled"))
            return services;

        if (!environment.IsDevelopment())
            throw new InvalidOperationException(
                "The diagnoses demonstration requires Development.");

        var connection = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException(
                "Configure the demonstration database connection.");

        var database = new MySqlConnectionStringBuilder(
            Environment.ExpandEnvironmentVariables(connection));

        if (database.Database != "skycrop_diagnoses_checks")
            throw new InvalidOperationException(
                "The demonstration requires skycrop_diagnoses_checks.");

        var plotId = configuration.GetValue<int>("Diagnoses:DemoPlotId");

        if (plotId <= 0)
            throw new InvalidOperationException(
                "Configure a positive Diagnoses:DemoPlotId.");

        services.AddHttpContextAccessor();
        services.Replace(
            ServiceDescriptor.Scoped<IPlotAccessService>(provider =>
                new LocalDemoPlotAccessService(
                    plotId,
                    provider.GetRequiredService<IHttpContextAccessor>())));

        return services;
    }
}