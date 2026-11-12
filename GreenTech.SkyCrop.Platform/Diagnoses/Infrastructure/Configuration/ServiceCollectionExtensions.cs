using GreenTech.SkyCrop.Platform.Diagnoses.Application.IntegrationServices;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Repositories;
using GreenTech.SkyCrop.Platform.Diagnoses.Infrastructure.ImageAnalysis;
using GreenTech.SkyCrop.Platform.Diagnoses.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace GreenTech.SkyCrop.Platform.Diagnoses.Infrastructure.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDiagnosesContext(
        this IServiceCollection services)
    {
        services.AddScoped<IDiagnosisRepository, DiagnosisRepository>();
        services.AddScoped<IImageAnalysis, SimulatedImageAnalysis>();

        return services;
    }
}