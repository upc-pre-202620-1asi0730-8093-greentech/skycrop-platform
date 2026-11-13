using GreenTech.SkyCrop.Platform.Drones.Infrastructure.Configuration;
using GreenTech.SkyCrop.Platform.Plots.Infrastructure.Configuration;
using GreenTech.SkyCrop.Platform.Resources.Errors;
using GreenTech.SkyCrop.Platform.Resources.Shared;
using GreenTech.SkyCrop.Platform.Shared.Domain.Repositories;
using GreenTech.SkyCrop.Platform.Shared.Infrastructure.Interfaces.AspNetCore.Configuration;
using GreenTech.SkyCrop.Platform.Shared.Infrastructure.Interfaces.AspNetCore.OpenApi;
using GreenTech.SkyCrop.Platform.Shared.Infrastructure.Mediator.Cortex.Configuration;
using GreenTech.SkyCrop.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using GreenTech.SkyCrop.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Interceptors;
using GreenTech.SkyCrop.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using GreenTech.SkyCrop.Platform.Shared.Infrastructure.Pipeline.Middleware.Components;
using Cortex.Mediator.Commands;
using Cortex.Mediator.DependencyInjection;
using GreenTech.SkyCrop.Platform.Reports.Application.CommandServices;
using GreenTech.SkyCrop.Platform.Reports.Application.Internal.CommandServices;
using GreenTech.SkyCrop.Platform.Reports.Application.Internal.QueryServices;
using GreenTech.SkyCrop.Platform.Reports.Application.QueryServices;
using GreenTech.SkyCrop.Platform.Reports.Domain.Repositories;
using GreenTech.SkyCrop.Platform.Reports.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using GreenTech.SkyCrop.Platform.Reports.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Scalar.AspNetCore;
using ProblemDetailsFactory = GreenTech.SkyCrop.Platform.Shared.Interfaces.Rest.ProblemDetails.ProblemDetailsFactory;
using GreenTech.SkyCrop.Platform.Diagnoses.Infrastructure.Configuration;
using GreenTech.SkyCrop.Platform.Diagnoses.Application.IntegrationServices;
using GreenTech.SkyCrop.Platform.Diagnoses.Infrastructure.Integration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MySql.Data.MySqlClient;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddControllers(options => options.Conventions.Add(new KebabCaseRouteNamingConvention()))
    .AddDataAnnotationsLocalization();

// Add ProblemDetails and ExceptionHandler services
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Add CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllPolicy",
        policy => policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

// Configure Database Context and route EF logs through the app logger pipeline.
builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
{
    var connectionStringTemplate = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionStringTemplate))
        throw new InvalidOperationException("Database connection string is not set in the configuration.");

    var connectionString = Environment.ExpandEnvironmentVariables(connectionStringTemplate);

    options.UseMySQL(connectionString)
        .UseLoggerFactory(serviceProvider.GetRequiredService<ILoggerFactory>())
        .AddInterceptors(serviceProvider.GetRequiredService<DispatchDomainEventsInterceptor>())
        .EnableDetailedErrors();

    if (builder.Environment.IsDevelopment())
        options.EnableSensitiveDataLogging();
});

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// Explicitly register IStringLocalizer for ErrorMessages and Commons
builder.Services.AddSingleton<IStringLocalizer<ErrorMessages>, StringLocalizer<ErrorMessages>>();
builder.Services.AddSingleton<IStringLocalizer<CommonMessages>, StringLocalizer<CommonMessages>>();
builder.Services.AddSingleton<IStringLocalizer<ReportsMessages>, StringLocalizer<ReportsMessages>>();

// Register the custom ProblemDetailsFactory
builder.Services.AddSingleton<ProblemDetailsFactory>();

// OpenAPI / Scalar documentation
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

// Dependency Injection

// Shared Bounded Context
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<DispatchDomainEventsInterceptor>();

//Reports Bounded Context
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<IReportCommandService, ReportCommandService>();
builder.Services.AddScoped<IReportQueryService, ReportQueryService>();
// Drones Bounded Context
builder.Services.AddDronesContext();

// Plots Bounded Context
builder.Services.AddPlotsContext();

// Diagnoses Bounded Context
builder.Services.AddDiagnosesContext();

if (builder.Configuration.GetValue<bool>("Diagnoses:LocalDemoEnabled"))
{
    if (!builder.Environment.IsDevelopment())
        throw new InvalidOperationException(
            "The diagnoses demonstration requires Development.");

    var connection = builder.Configuration
        .GetConnectionString("DefaultConnection");

    if (string.IsNullOrWhiteSpace(connection))
        throw new InvalidOperationException(
            "Configure the demonstration database connection.");

    var database = new MySqlConnectionStringBuilder(
        Environment.ExpandEnvironmentVariables(connection));

    if (database.Database != "skycrop_diagnoses_checks")
        throw new InvalidOperationException(
            "The demonstration requires skycrop_diagnoses_checks.");

    var demoPlotId = builder.Configuration.GetValue<int>(
        "Diagnoses:DemoPlotId");

    if (demoPlotId <= 0)
        throw new InvalidOperationException(
            "Configure a positive Diagnoses:DemoPlotId.");

    builder.Services.AddHttpContextAccessor();

    builder.Services.Replace(
        ServiceDescriptor.Scoped<IPlotAccessService>(provider =>
            new LocalDemoPlotAccessService(
                demoPlotId,
                provider.GetRequiredService<IHttpContextAccessor>())));
}

// Mediator Configuration
builder.Services.AddScoped(typeof(ICommandPipelineBehavior<>), typeof(LoggingCommandBehavior<>));
builder.Services.AddScoped(typeof(ICommandPipelineBehavior<>), typeof(ExceptionHandlingCommandBehavior<>));
builder.Services.AddScoped(typeof(ICommandPipelineBehavior<>), typeof(TransactionCommandBehavior<>));

// Add Cortex Mediator for Command and Event Handling
builder.Services.AddCortexMediator([typeof(Program)]);

var app = builder.Build();

// Apply pending migrations on startup (safe to call even when schema is up to date)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    context.Database.Migrate();
}

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

var supportedCultures = new[] { "en-US", "en", "es-419", "es" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();

    app.MapGet("/", () => Results.Redirect("/scalar"))
        .ExcludeFromDescription();
}

// Apply CORS Policy
app.UseCors("AllowAllPolicy");

app.UseHttpsRedirection();

app.MapControllers();

app.Run();