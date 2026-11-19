using Cortex.Mediator.DependencyInjection;
using Cortex.Mediator.Notifications;
using Cortex.Mediator;
using GreenTech.SkyCrop.Platform.Diagnoses.Application.Acl;
using GreenTech.SkyCrop.Platform.Diagnoses.Application.IntegrationServices;
using GreenTech.SkyCrop.Platform.Diagnoses.Application.Internal.CommandServices;
using GreenTech.SkyCrop.Platform.Diagnoses.Application.Internal.QueryServices;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Commands;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Entities;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Events;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Queries;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.ValueObjects;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Repositories;
using GreenTech.SkyCrop.Platform.Diagnoses.Infrastructure.Configuration;
using GreenTech.SkyCrop.Platform.Diagnoses.Infrastructure.ImageAnalysis;
using GreenTech.SkyCrop.Platform.Diagnoses.Infrastructure.Integration;
using GreenTech.SkyCrop.Platform.Diagnoses.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using GreenTech.SkyCrop.Platform.Diagnoses.Interfaces.Events;
using GreenTech.SkyCrop.Platform.Diagnoses.Interfaces.Rest.Transform;
using GreenTech.SkyCrop.Platform.Drones.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Drones.Domain.Model.Commands;
using GreenTech.SkyCrop.Platform.Drones.Domain.Model.Entities;
using GreenTech.SkyCrop.Platform.Drones.Domain.Model;
using GreenTech.SkyCrop.Platform.Plots.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Plots.Domain.Model.Commands;
using GreenTech.SkyCrop.Platform.Shared.Application.Model;
using GreenTech.SkyCrop.Platform.Shared.Domain.Model.Errors;
using GreenTech.SkyCrop.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using GreenTech.SkyCrop.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Interceptors;
using GreenTech.SkyCrop.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;

// ===== EXECUTION MODE =====
if (args.Contains("--seed-demo"))
{
    await DiagnosisDemoSeed.RunAsync();
    return;
}

if (args.Contains("--mysql"))
{
    await DiagnosisPersistenceChecks.RunAsync();
    return;
}

// ===== COUNTERS AND TEST HELPERS =====
var passed = 0;
var failed = 0;

// Fixed dates make tests independent of the execution date.
var requestedAt = new DateTimeOffset( 2026, 10, 10, 12, 0, 0, TimeSpan.Zero);

Diagnosis CreateDiagnosis(string scenario = "WATER_STRESS") =>
    new(1, 2, new[] { 10, 11 }, scenario, requestedAt.AddHours(-1), requestedAt);

Anomaly CreateAnomaly(string type = "WATER_STRESS") =>
    new(type, "MEDIUM", "Simulated anomaly", "North sector");

void Check(bool condition, string message)
{
    if (!condition)
        throw new Exception(message);
}

void Throws<TException>(Action action) where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }

    throw new Exception( $"Expected {typeof(TException).Name}, but no exception was thrown.");
}

void Test(string name, Action action)
{
    try
    {
        action();
        passed++;
        Console.WriteLine($"PASS: {name}");
    }
    catch (Exception exception)
    {
        failed++;
        Console.WriteLine($"FAIL: {name}");
        Console.WriteLine($"  {exception.Message}");
    }
}

var invalidCoordinates = new (double? Latitude, double? Longitude)[]
{
    (0, null), (null, 0), (91, 0), (0, -181), (double.NaN, 0)
};

// ===== DOMAIN CHECKS =====
Test("NDVI accepts boundary values and compares by value", () =>
{
    foreach (var value in new[] { -1d, 0d, 0.4d, 1d })
        Check(new Ndvi(value).Value == value, "NDVI changed its value.");

    Check(new Ndvi(0.4) == new Ndvi(0.4), "Equal NDVI values must be equal.");
});

Test("NDVI rejects out-of-range and non-finite values", () =>
{
    foreach (var value in new[]
             {
                 -1.01, 1.01, double.NaN, double.PositiveInfinity, double.NegativeInfinity
             })
    {
        Throws<ArgumentException>(() => new Ndvi(value));
    }
});

Test("Anomalies support frontend types and severity levels", () =>
{
    foreach (var type in new[]
             {
                 "WATER_STRESS", "PEST", "FERTILIZER_DEFICIENCY", "OTHER"
             })
    {
        foreach (var severity in new[] { "LOW", "MEDIUM", "HIGH", "CRITICAL" })
        {
            var anomaly = new Anomaly( type, severity, "Description", "Location");

            Check(anomaly.Type == type && anomaly.Severity == severity, "Unexpected anomaly classification.");
        }
    }

    var legacy = CreateAnomaly(" fertilizer_deficiencity ");
    Check(legacy.Type == "FERTILIZER_DEFICIENCY", "Legacy spelling was not normalized.");
});

Test("Anomalies reject invalid classifications and blank descriptions", () =>
{
    Throws<ArgumentException>(() => CreateAnomaly("UNKNOWN"));
    Throws<ArgumentException>(() =>
        new Anomaly("PEST", "UNKNOWN", "Description", "Location"));
    Throws<ArgumentException>(() =>
        new Anomaly("PEST", "HIGH", " ", "Location"));
});

Test("Anomalies accept missing coordinates or a valid coordinate pair", () =>
{
    var withoutCoordinates = CreateAnomaly();
    Check(withoutCoordinates.Latitude is null &&
          withoutCoordinates.Longitude is null, "Missing coordinates should remain null.");

    var located = new Anomaly( "PEST", "HIGH", "Description", "Location", -12.0464, -77.0428);

    Check(located.Latitude == -12.0464 && located.Longitude == -77.0428, "Coordinates were not preserved.");
});

Test("Anomalies reject incomplete and invalid coordinates", () =>
{
    foreach (var coordinates in invalidCoordinates)
    {
        Throws<ArgumentException>(() =>
            new Anomaly( "PEST", "HIGH", "Description", "Location",
                coordinates.Latitude, coordinates.Longitude));
    }
});

Test("A new diagnosis is pending and has no completed results", () =>
{
    var diagnosis = CreateDiagnosis();

    Check(diagnosis.Status == EDiagnosisStatus.PENDING, "Initial status must be PENDING.");
    Check(diagnosis.GeneratedAt is null &&
          diagnosis.AverageNdvi is null &&
          diagnosis.Anomalies.Count == 0, "Pending diagnosis must not contain completed results.");
    Check(diagnosis.Source == "SIMULATED", "Simulation must be identified explicitly.");
});

Test("Diagnosis rejects invalid references and image collections", () =>
{
    Throws<ArgumentException>(() =>
        new Diagnosis(0, 2, new[] { 10 }, "HEALTHY", requestedAt, requestedAt));
    Throws<ArgumentException>(() =>
        new Diagnosis(1, 0, new[] { 10 }, "HEALTHY", requestedAt, requestedAt));

    foreach (var images in new[]
             {
                 new[] { 0 }, new[] { 10, 10 }, Enumerable.Range(1, 101).ToArray()
             })
    {
        Throws<ArgumentException>(() =>
            new Diagnosis(1, 2, images, "HEALTHY", requestedAt, requestedAt));
    }
});

Test("Diagnosis rejects invalid scenarios and monitoring dates", () =>
{
    Throws<ArgumentException>(() => CreateDiagnosis("UNKNOWN"));
    Throws<ArgumentException>(() =>
        new Diagnosis(1, 2, new[] { 10 }, "HEALTHY", requestedAt.AddMinutes(1), requestedAt));
});

Test("Diagnosis copies image identifiers from the caller", () =>
{
    var images = new List<int> { 10, 11 };
    var diagnosis = new Diagnosis( 1, 2, images, "HEALTHY", requestedAt, requestedAt);

    images.Clear();

    Check(diagnosis.ImageIds.SequenceEqual(new[] { 10, 11 }),
        "Changing the input collection changed the diagnosis.");
});

Test("Pending diagnosis cannot complete before processing", () =>
{
    var diagnosis = CreateDiagnosis();

    Throws<InvalidOperationException>(() =>
        diagnosis.CompleteSimulated( "Result", new Ndvi(0.4), new[] { CreateAnomaly() }, requestedAt));

    Check(diagnosis.Status == EDiagnosisStatus.PENDING, "Invalid completion changed the status.");
});

Test("Water stress diagnosis completes with results and monitoring date", () =>
{
    var diagnosis = CreateDiagnosis();
    var monitoredAt = diagnosis.MonitoredAt;
    var generatedAt = requestedAt.AddMinutes(1);

    diagnosis.StartProcessing();
    diagnosis.CompleteSimulated( "Simulated water stress", new Ndvi(0.4),
        new[] { CreateAnomaly() }, generatedAt);

    Check(diagnosis.Status == EDiagnosisStatus.COMPLETED, "Diagnosis did not complete.");
    Check(diagnosis.AverageNdvi?.Value == 0.4 &&
          diagnosis.Anomalies.Count == 1 &&
          diagnosis.GeneratedAt == generatedAt, "Completed results were not preserved.");
    Check(diagnosis.MonitoredAt == monitoredAt, "Completion changed the monitoring date.");

    Throws<InvalidOperationException>(() => diagnosis.StartProcessing());
    Throws<InvalidOperationException>(() => diagnosis.Fail("Late failure"));
    Throws<InvalidOperationException>(() =>
        diagnosis.CompleteSimulated( "Replacement", new Ndvi(0.4), new[] { CreateAnomaly() }, generatedAt));
});

Test("Healthy diagnosis completes without anomalies", () =>
{
    var diagnosis = CreateDiagnosis("HEALTHY");
    diagnosis.StartProcessing();

    diagnosis.CompleteSimulated( "Simulated healthy crop", new Ndvi(0.8),
        Array.Empty<Anomaly>(), requestedAt);

    Check(diagnosis.Status == EDiagnosisStatus.COMPLETED &&
          diagnosis.Anomalies.Count == 0, "Healthy result should complete without anomalies.");
});

Test("Rejected results leave processing diagnosis unchanged", () =>
{
    var diagnosis = CreateDiagnosis();
    diagnosis.StartProcessing();

    Throws<ArgumentException>(() =>
        diagnosis.CompleteSimulated( "Wrong scenario", new Ndvi(0.55),
            new[] { CreateAnomaly("PEST") }, requestedAt));

    Throws<ArgumentException>(() =>
        diagnosis.CompleteSimulated( "Missing anomaly", new Ndvi(0.4), Array.Empty<Anomaly>(), requestedAt));

    Throws<ArgumentException>(() =>
        diagnosis.CompleteSimulated( "Invalid date", new Ndvi(0.4),
            new[] { CreateAnomaly() }, requestedAt.AddMinutes(-1)));

    Check(diagnosis.Status == EDiagnosisStatus.PROCESSING &&
          diagnosis.AverageNdvi is null &&
          diagnosis.GeneratedAt is null &&
          diagnosis.Anomalies.Count == 0 &&
          diagnosis.Observations == string.Empty, "Rejected completion partially modified the diagnosis.");
});

Test("Healthy diagnosis rejects anomalies", () =>
{
    var diagnosis = CreateDiagnosis("HEALTHY");
    diagnosis.StartProcessing();

    Throws<ArgumentException>(() =>
        diagnosis.CompleteSimulated( "Invalid healthy result", new Ndvi(0.8),
            new[] { CreateAnomaly() }, requestedAt));
});

Test("Diagnosis rejects duplicate anomaly instances", () =>
{
    var diagnosis = CreateDiagnosis();
    var anomaly = CreateAnomaly();
    diagnosis.StartProcessing();

    Throws<ArgumentException>(() =>
        diagnosis.CompleteSimulated( "Duplicate results", new Ndvi(0.4),
            new[] { anomaly, anomaly }, requestedAt));
});

Test("Pending and processing diagnoses can fail without completed results", () =>
{
    foreach (var startProcessing in new[] { false, true })
    {
        var diagnosis = CreateDiagnosis();

        if (startProcessing)
            diagnosis.StartProcessing();

        diagnosis.Fail("Analysis unavailable");

        Check(diagnosis.Status == EDiagnosisStatus.FAILED &&
              diagnosis.FailureReason == "Analysis unavailable" &&
              diagnosis.AverageNdvi is null &&
              diagnosis.GeneratedAt is null &&
              diagnosis.Anomalies.Count == 0, "Failure must preserve its reason without completed results.");

        Throws<InvalidOperationException>(() => diagnosis.StartProcessing());
        Throws<InvalidOperationException>(() => diagnosis.Fail("Another failure"));
        Throws<InvalidOperationException>(() =>
            diagnosis.CompleteSimulated( "Late result", new Ndvi(0.4),
                new[] { CreateAnomaly() }, requestedAt));
    }
});

// ===== SIMULATED ANALYSIS CHECKS =====
var analyzer = new SimulatedImageAnalysis();

ImageAnalysisResult Analyze(string scenario) =>
    analyzer.AnalyzeAsync( new ImageAnalysisInput( scenario, "North sector", -12.0464, -77.0428))
    .GetAwaiter().GetResult();

var analysisCases = new[]
{
    (Scenario: "HEALTHY", Ndvi: 0.8, Severity: ""), (Scenario: "WATER_STRESS", Ndvi: 0.4, Severity: "MEDIUM"),
    (Scenario: "PEST", Ndvi: 0.55, Severity: "HIGH")
};

foreach (var testCase in analysisCases)
{
    Test($"{testCase.Scenario} analysis returns valid results and completes a diagnosis", () =>
    {
        var result = Analyze(testCase.Scenario);
        var expectedAnomalies = testCase.Scenario == "HEALTHY" ? 0 : 1;

        Check(result.AverageNdvi.Value == testCase.Ndvi &&
              result.Anomalies.Count == expectedAnomalies, $"Unexpected results for {testCase.Scenario}.");

        if (expectedAnomalies > 0)
        {
            var anomaly = result.Anomalies.Single();

            Check(anomaly.Type == testCase.Scenario &&
                  anomaly.Severity == testCase.Severity &&
                  anomaly.Location == "North sector" &&
                  anomaly.Latitude == -12.0464 &&
                  anomaly.Longitude == -77.0428, "Analysis must preserve classification and location.");
        }

        var diagnosis = CreateDiagnosis(testCase.Scenario);
        diagnosis.StartProcessing();
        diagnosis.CompleteSimulated( result.Observations, result.AverageNdvi, result.Anomalies,
            requestedAt.AddMinutes(1));

        Check(diagnosis.Status == EDiagnosisStatus.COMPLETED &&
              diagnosis.Source == "SIMULATED" &&
              diagnosis.AverageNdvi == result.AverageNdvi &&
              diagnosis.Anomalies.Count == result.Anomalies.Count,
            "Analysis results must complete their matching diagnosis.");
    });
}

Test("Analysis without coordinates preserves the diagnosis results", () =>
{
    var result = analyzer.AnalyzeAsync( new ImageAnalysisInput("PEST", "North sector", null, null))
        .GetAwaiter().GetResult();

    var anomaly = result.Anomalies.Single();

    Check(result.AverageNdvi.Value == 0.55 &&
          anomaly.Latitude is null &&
          anomaly.Longitude is null, "Missing coordinates must not remove the analysis result.");
});

Test("Analysis rejects unknown scenarios and invalid coordinates", () =>
{
    Throws<ArgumentException>(() => Analyze("UNKNOWN"));

    foreach (var coordinates in invalidCoordinates)
    {
        Throws<ArgumentException>(() =>
            analyzer.AnalyzeAsync(new ImageAnalysisInput( "HEALTHY", "North sector",
                    coordinates.Latitude, coordinates.Longitude)).GetAwaiter().GetResult());
    }
});
Test("Analysis respects cancellation", () =>
{
    using var cancellation = new CancellationTokenSource();
    cancellation.Cancel();

    Throws<OperationCanceledException>(() =>
        analyzer.AnalyzeAsync( new ImageAnalysisInput("HEALTHY", "North sector", null, null),
            cancellation.Token).GetAwaiter().GetResult());
});

// ===== EF MODEL CHECKS =====
// These contexts inspect EF mappings without connecting to MySQL.
AppDbContext CreateModelContext() => new(
    new DbContextOptionsBuilder<AppDbContext>().UseMySQL(
            "server=127.0.0.1;database=model_only;user=unused;password=unused;").Options);

using var diagnosisContext = CreateModelContext();

Test("EF maps diagnoses and their history index", () =>
{
    var mapping = diagnosisContext.Model.FindEntityType(typeof(Diagnosis))
                  ?? throw new InvalidOperationException( "Diagnosis is missing from the EF model.");

    Check(mapping.GetTableName() == "diagnoses", "Unexpected diagnosis table.");

    Check(mapping.GetIndexes().Any(index =>
            index.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { "PlotId", "MonitoredAt", "Id" })), "The history index is missing.");

    Check(mapping.FindProperty(nameof(Diagnosis.GeneratedAt))!.IsNullable,
        "Incomplete diagnoses must allow a null generation date.");
});

Test("EF converts NDVI to a number and restores its value object", () =>
{
    var mapping = diagnosisContext.Model.FindEntityType(typeof(Diagnosis))!;
    var converter = mapping.FindProperty(nameof(Diagnosis.AverageNdvi))!.GetTypeMapping().Converter!;

    var stored = converter.ConvertToProvider(new Ndvi(0.4));
    var restored = (Ndvi)converter.ConvertFromProvider(stored)!;

    Check(stored is double value && value == 0.4, "NDVI must be stored as a number.");
    Check(restored == new Ndvi(0.4), "NDVI was not restored correctly.");
    Check(converter.ConvertToProvider(null) is null, "Missing NDVI must remain null.");
});

Test("EF preserves image identifiers and creates independent snapshots", () =>
{
    var mapping = diagnosisContext.Model.FindEntityType(typeof(Diagnosis))!;
    var property = mapping.FindProperty("_imageIds")!;
    var converter = property.GetTypeMapping().Converter!;
    var comparer = property.GetValueComparer()!;

    var original = new List<int> { 10, 11 };
    var stored = converter.ConvertToProvider(original);
    var restored = (List<int>)converter.ConvertFromProvider(stored)!;
    var snapshot = (List<int>)comparer.Snapshot(original)!;

    Check(restored.SequenceEqual(original), "Image references changed during conversion.");

    original.Add(12);

    Check(snapshot.SequenceEqual(new[] { 10, 11 }), "The change-tracking snapshot shares the original list.");
    Check(!comparer.Equals(original, snapshot), "The comparer did not detect a list modification.");
});

Test("EF maps anomalies as owned entities of a diagnosis", () =>
{
    var mapping = diagnosisContext.Model.FindEntityType(typeof(Diagnosis))!;
    var anomaly = mapping.FindNavigation(nameof(Diagnosis.Anomalies))!.TargetEntityType;

    Check(anomaly.GetTableName() == "diagnosis_anomalies", "Unexpected anomaly table.");

    var ownership = anomaly.GetForeignKeys().Single(foreignKey => foreignKey.IsOwnership);

    Check(ownership.Properties.Single().Name == nameof(Anomaly.DiagnosisId),
        "Anomaly ownership must use DiagnosisId.");

    Check(anomaly.FindProperty(nameof(Anomaly.Latitude))!.IsNullable &&
          anomaly.FindProperty(nameof(Anomaly.Longitude))!.IsNullable,
        "Anomalies without coordinates must remain persistable.");
});

Test("EF normalizes monitoring dates to UTC without changing the instant", () =>
{
    var mapping = diagnosisContext.Model.FindEntityType(typeof(Diagnosis))!;
    var converter = mapping.FindProperty(nameof(Diagnosis.MonitoredAt))!.GetTypeMapping().Converter!;

    var localTime = new DateTimeOffset( 2026, 10, 10, 7, 0, 0, TimeSpan.FromHours(-5));

    var stored = (DateTime)converter.ConvertToProvider(localTime)!;
    var restored = (DateTimeOffset)converter.ConvertFromProvider(stored)!;

    Check(stored == localTime.UtcDateTime, "Monitoring date was not converted to UTC.");
    Check(restored == localTime && restored.Offset == TimeSpan.Zero,
        "UTC conversion changed the monitoring instant.");
});

// ===== DEPENDENCY AND REPOSITORY CHECKS =====
ServiceProvider CreateModelServices()
{
    var services = new ServiceCollection();
    services.AddDbContext<AppDbContext>(options => options.UseMySQL(
        "server=127.0.0.1;database=model_only;user=unused;password=unused;"));
    services.AddDiagnosesContext();
    return services.BuildServiceProvider(new ServiceProviderOptions
    {
        ValidateScopes = true, ValidateOnBuild = true
    });
}

Test("Diagnosis dependencies resolve within an application scope", () =>
{
    using var provider = CreateModelServices();

    using var scope = provider.CreateScope();

    var repository = scope.ServiceProvider.GetRequiredService<IDiagnosisRepository>();

    var analysis = scope.ServiceProvider.GetRequiredService<IImageAnalysis>();

    Check(repository is DiagnosisRepository, "The diagnosis repository was not registered correctly.");

    Check(analysis is SimulatedImageAnalysis, "The simulated analysis service was not registered correctly.");

    Check(ReferenceEquals( repository, scope.ServiceProvider.GetRequiredService<IDiagnosisRepository>()),
        "A scope must reuse its repository instance.");
});

Test("Repository stages a completed diagnosis and its anomalies", () =>
{
    using var context = CreateModelContext();

    var repository = new DiagnosisRepository(context);
    var diagnosis = CreateDiagnosis();
    var result = Analyze("WATER_STRESS");

    diagnosis.StartProcessing();
    diagnosis.CompleteSimulated( result.Observations, result.AverageNdvi, result.Anomalies,
        requestedAt.AddMinutes(1));

    repository.AddAsync(diagnosis).GetAwaiter().GetResult();

    Check(context.Entry(diagnosis).State == EntityState.Added, "The diagnosis was not staged for insertion.");

    var anomalyEntries = context.ChangeTracker.Entries<Anomaly>().ToArray();

    Check(anomalyEntries.Length == 1 &&
          anomalyEntries[0].State == EntityState.Added,
        "The owned anomaly was not staged with its diagnosis.");

    // No SaveChanges call: this test does not write to MySQL.
});

Test("Repository rejects invalid plot identifiers before querying MySQL", () =>
{
    using var context = CreateModelContext();

    var repository = new DiagnosisRepository(context);

    foreach (var plotId in new[] { 0, -1 })
    {
        Throws<ArgumentException>(() =>
            repository.FindByPlotIdAsync(plotId).GetAwaiter().GetResult());
    }
});

// ===== ACCESS CHECKS =====
LocalDemoPlotAccessService CreateLocalDemoAccess(IPAddress? address)
{
    var accessor = new HttpContextAccessor();
    if (address is not null)
    {
        accessor.HttpContext = new DefaultHttpContext();
        accessor.HttpContext.Connection.RemoteIpAddress = address;
    }
    return new LocalDemoPlotAccessService(7, accessor);
}

Test("Unconfigured access never grants permission to a plot", () =>
{
    using var provider = CreateModelServices();

    using var scope = provider.CreateScope();

    var access = scope.ServiceProvider.GetRequiredService<IPlotAccessService>();

    var result = access.CheckAccessAsync(1).GetAwaiter().GetResult();

    Check(result.IsFailure &&
          result.DomainError.Code == "Diagnoses.AccessNotConfigured",
        "Missing IAM integration must not grant access.");
});

Test("Unconfigured access does not disguise failure as an empty history", () =>
{
    IPlotAccessService access = new UnconfiguredPlotAccessService();

    var result = access.GetAccessiblePlotIdsAsync().GetAwaiter().GetResult();

    Check(result.IsFailure &&
          result.Value is null &&
          result.DomainError.Code == "Diagnoses.AccessNotConfigured",
        "Missing permissions integration must return a failure.");
});

Test("Access checks reject invalid plot identifiers", () =>
{
    IPlotAccessService access = new UnconfiguredPlotAccessService();

    foreach (var plotId in new[] { 0, -1 })
    {
        var result = access.CheckAccessAsync(plotId).GetAwaiter().GetResult();

        Check(result.IsFailure &&
              result.DomainError.Code == "Diagnoses.InvalidPlotId",
            "Invalid plot identifiers must be rejected.");
    }
});

Test("Access checks respect cancellation", () =>
{
    IPlotAccessService access = new UnconfiguredPlotAccessService();

    using var cancellation = new CancellationTokenSource();
    cancellation.Cancel();

    Throws<OperationCanceledException>(() =>
        access.CheckAccessAsync(1, cancellation.Token).GetAwaiter().GetResult());

    Throws<OperationCanceledException>(() =>
        access.GetAccessiblePlotIdsAsync(cancellation.Token).GetAwaiter().GetResult());
});

Test("Local demo grants access only to its configured plot", () =>
{
    var access = CreateLocalDemoAccess(IPAddress.Loopback);

    var allowed = access.CheckAccessAsync(7).GetAwaiter().GetResult();
    var denied = access.CheckAccessAsync(8).GetAwaiter().GetResult();

    Check(allowed.IsSuccess && denied.IsFailure,
        "Local demo permissions must be limited to the configured plot.");
});

Test("Local demo rejects non-local requests", () =>
{
    var access = CreateLocalDemoAccess(IPAddress.Parse("192.0.2.10"));

    var detail = access.CheckAccessAsync(7).GetAwaiter().GetResult();
    var list = access.GetAccessiblePlotIdsAsync().GetAwaiter().GetResult();

    Check(detail.IsFailure && list.IsFailure &&
          detail.DomainError.Code == "Diagnoses.LocalDemoOnly",
        "Remote requests must not obtain demonstration permissions.");
});

Test("Local demo rejects calls without an HTTP request", () =>
{
    var access = CreateLocalDemoAccess(null);

    var result = access.CheckAccessAsync(7).GetAwaiter().GetResult();

    Check(result.IsFailure, "Missing HTTP context must not grant demonstration access.");
});

Test("Normal development startup resolves plot access without demo settings", () =>
{
    foreach (var environmentName in new[] { "Development", "Production", "Staging" })
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseMySQL(
            "server=127.0.0.1;database=model_only;user=unused;password=unused;"));
        services.AddDiagnosesContext();
        services.AddDiagnosesDevelopmentAccess(
            new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build(),
            new Microsoft.Extensions.Hosting.Internal.HostingEnvironment { EnvironmentName = environmentName });
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = provider.CreateScope();
        var access = scope.ServiceProvider.GetRequiredService<IPlotAccessService>();
        Check(environmentName == "Development"
                ? access is DevelopmentPlotAccessService
                : access is UnconfiguredPlotAccessService,
            "Only Development may automatically enable local plot access.");
    }
});

Test("Development setup preserves an access service supplied by another integration", () =>
{
    var services = new ServiceCollection();
    IPlotAccessService supplied = new LocalDemoPlotAccessService(7, new HttpContextAccessor());
    services.AddSingleton(supplied);
    services.AddDiagnosesContext();
    var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build();
    services.AddDiagnosesDevelopmentAccess(configuration,
        new Microsoft.Extensions.Hosting.Internal.HostingEnvironment { EnvironmentName = "Development" });
    using var provider = services.BuildServiceProvider();
    Check(ReferenceEquals(supplied, provider.GetRequiredService<IPlotAccessService>()),
        "Local development configuration must not override the team's access integration.");
});

Test("Development access rejects remote and missing HTTP requests before database access", () =>
{
    using var context = CreateModelContext();
    foreach (var address in new IPAddress?[] { null, IPAddress.Parse("192.0.2.10") })
    {
        var http = new DefaultHttpContext();
        http.Connection.RemoteIpAddress = address;
        var access = new DevelopmentPlotAccessService(context, new HttpContextAccessor { HttpContext = http });
        Check(access.CheckAccessAsync(1).GetAwaiter().GetResult().IsFailure &&
              access.GetAccessiblePlotIdsAsync().GetAwaiter().GetResult().IsFailure,
            "Development access must be limited to local requests.");
    }
});

Test("Development access validates plot IDs and respects cancellation", () =>
{
    using var context = CreateModelContext();
    var http = new DefaultHttpContext();
    http.Connection.RemoteIpAddress = IPAddress.Loopback;
    var access = new DevelopmentPlotAccessService(context, new HttpContextAccessor { HttpContext = http });
    Check(access.CheckAccessAsync(0).GetAwaiter().GetResult().IsFailure, "Invalid plot IDs must be rejected.");
    using var cancellation = new CancellationTokenSource();
    cancellation.Cancel();
    Throws<OperationCanceledException>(() =>
        access.CheckAccessAsync(1, cancellation.Token).GetAwaiter().GetResult());
    Throws<OperationCanceledException>(() =>
        access.GetAccessiblePlotIdsAsync(cancellation.Token).GetAwaiter().GetResult());
});

Test("Standalone diagnosis has no external references", () =>
{
    var diagnosis = new Diagnosis("WATER_STRESS", requestedAt);
    Check(diagnosis.PlotId is null && diagnosis.FlightId is null && diagnosis.ImageIds.Count == 0,
        "Standalone diagnoses must not invent external references.");
    using var model = CreateModelContext();
    var entity = model.Model.FindEntityType(typeof(Diagnosis))!;
    Check(!entity.GetForeignKeys().Any(), "Diagnosis must not require other context tables.");
});

// ===== MAP CHECKS =====
Anomaly LocatedMapAnomaly() => new(
    "WATER_STRESS", "MEDIUM", "Simulated water stress.", "North sector", -12.0464, -77.0428);

var mapCases = new ( string Name, string Scenario, double Ndvi, string Status, string? Reason, int Unlocated,
    Anomaly[] Anomalies)[]
{
    ( "Map preserves located results and provides their legend", "WATER_STRESS", 0.4, "AVAILABLE", null, 0,
        [LocatedMapAnomaly()]
    ), ( "Map never fabricates missing coordinates",
        "WATER_STRESS", 0.4, "UNAVAILABLE", "MISSING_COORDINATES", 1, [CreateAnomaly()]
    ), ( "Partial map preserves unlocated anomalies in the diagnosis",
        "WATER_STRESS", 0.4, "PARTIAL", null, 1, [LocatedMapAnomaly(), CreateAnomaly()]
    ), ( "Healthy diagnosis does not invent risk points", "HEALTHY", 0.8, "UNAVAILABLE", "NO_ANOMALIES", 0, []
    )
};

foreach (var testCase in mapCases)
{
    Test(testCase.Name, () =>
    {
        var diagnosis = CreateDiagnosis(testCase.Scenario);
        diagnosis.StartProcessing();
        diagnosis.CompleteSimulated( "Simulated map results.", new Ndvi(testCase.Ndvi), testCase.Anomalies,
            requestedAt.AddMinutes(1));

        var map = DiagnosisMapResourceFromEntityAssembler.ToResourceFromEntity(diagnosis);

        Check(map.Status == testCase.Status &&
              map.UnavailableReason == testCase.Reason &&
              map.Source == "SIMULATED", "Unexpected map availability or source.");

        Check(map.TotalAnomalies == testCase.Anomalies.Length &&
              map.UnlocatedAnomalies == testCase.Unlocated &&
              map.Points.Count == testCase.Anomalies.Length - testCase.Unlocated,
            "Map counts must distinguish located and unlocated results.");

        foreach (var point in map.Points)
        {
            Check(point.Latitude == -12.0464 &&
                  point.Longitude == -77.0428, "Map coordinates must preserve the diagnosis results.");

            Check(map.Legend.Any(item =>
                    item.Severity == point.Severity &&
                    item.Color == "#ac7200"), "Located results must have their severity legend.");
        }

        Check(diagnosis.Status == EDiagnosisStatus.COMPLETED &&
              diagnosis.Anomalies.Count == testCase.Anomalies.Length &&
              diagnosis.AverageNdvi!.Value == testCase.Ndvi,
            "Map generation must preserve diagnosis results.");
    });
}

Test("Unfinished and failed diagnoses do not expose completed map data", () =>
{
    var diagnosis = CreateDiagnosis();

    void CheckUnavailable()
    {
        var map = DiagnosisMapResourceFromEntityAssembler.ToResourceFromEntity(diagnosis);

        Check(map.Status == "UNAVAILABLE" &&
              map.UnavailableReason == "DIAGNOSIS_NOT_COMPLETED" &&
              map.Points.Count == 0, "Only completed diagnoses may expose map results.");
    }

    CheckUnavailable();
    diagnosis.StartProcessing();
    CheckUnavailable();
    diagnosis.Fail("Simulated analysis failure.");
    CheckUnavailable();
});

// ===== DOMAIN EVENT CHECKS =====
Test("Diagnosis records request and processing events once", () =>
{
    var diagnosis = CreateDiagnosis();

    var requested = diagnosis.DomainEvents.OfType<DiagnosisRequestedEvent>().Single();

    Check(requested.PlotId == diagnosis.PlotId &&
          requested.FlightId == diagnosis.FlightId &&
          requested.RequestedAt == requestedAt, "Request event must identify the monitoring request.");

    diagnosis.ClearDomainEvents();
    diagnosis.StartProcessing();

    Check(diagnosis.DomainEvents.Count == 1 &&
          diagnosis.DomainEvents.Single()
              is DiagnosisProcessingStartedEvent, "Starting processing must record one event.");

    Throws<InvalidOperationException>( () => diagnosis.StartProcessing());

    Check(diagnosis.DomainEvents.Count == 1, "Rejected transitions must not add another event.");
});

Test("Completed diagnosis records generated and anomaly facts", () =>
{
    var diagnosis = CreateDiagnosis();
    diagnosis.StartProcessing();
    diagnosis.ClearDomainEvents();

    diagnosis.CompleteSimulated( "Simulated water stress.", new Ndvi(0.4), [CreateAnomaly()],
        requestedAt.AddMinutes(1));

    var generated = diagnosis.DomainEvents.OfType<DiagnosisGeneratedEvent>().Single();

    var detected = diagnosis.DomainEvents.OfType<AgronomicAnomalyDetectedEvent>().Single();

    Check(generated.MonitoredAt == diagnosis.MonitoredAt &&
          generated.GeneratedAt == requestedAt.AddMinutes(1) &&
          generated.AverageNdvi == 0.4 &&
          generated.AnomalyCount == 1 &&
          generated.Source == "SIMULATED", "Generated event must preserve monitoring and result data.");

    Check(detected.Type == "WATER_STRESS" &&
          detected.Severity == "MEDIUM" &&
          detected.Source == "SIMULATED", "Anomaly event must preserve its classification and source.");

    Check(diagnosis.DomainEvents.Count == 2, "One anomaly must produce two completion-related events.");

    Throws<InvalidOperationException>(() =>
        diagnosis.CompleteSimulated( "Repeated completion.", new Ndvi(0.4), [CreateAnomaly()],
            requestedAt.AddMinutes(2)));

    Check(diagnosis.DomainEvents.Count == 2, "Repeated completion must not duplicate events.");

    diagnosis.ClearDomainEvents();

    Check(diagnosis.DomainEvents.Count == 0, "Published events must be removable from the aggregate.");
});

Test("Healthy completion does not record anomaly events", () =>
{
    var diagnosis = CreateDiagnosis("HEALTHY");
    diagnosis.StartProcessing();
    diagnosis.ClearDomainEvents();

    diagnosis.CompleteSimulated( "Healthy simulated result.", new Ndvi(0.8), Array.Empty<Anomaly>(),
        requestedAt.AddMinutes(1));

    Check(diagnosis.DomainEvents.Count == 1 &&
          diagnosis.DomainEvents.Single()
              is DiagnosisGeneratedEvent { AnomalyCount: 0 },
        "Healthy completion must only record the generated event.");
});

Test("Invalid results do not record completion events", () =>
{
    var diagnosis = CreateDiagnosis();
    diagnosis.StartProcessing();
    diagnosis.ClearDomainEvents();

    Throws<ArgumentException>(() =>
        diagnosis.CompleteSimulated( "Invalid scenario results.", new Ndvi(0.4), [CreateAnomaly("PEST")],
            requestedAt.AddMinutes(1)));

    Check(diagnosis.Status == EDiagnosisStatus.PROCESSING &&
          diagnosis.DomainEvents.Count == 0, "Rejected results must preserve state without success events.");
});

Test("Failed diagnoses record failure without success events", () =>
{
    foreach (var startProcessing in new[] { false, true })
    {
        var diagnosis = CreateDiagnosis();

        if (startProcessing)
            diagnosis.StartProcessing();

        diagnosis.ClearDomainEvents();
        diagnosis.Fail("Analysis could not be completed.");

        Check(diagnosis.DomainEvents.Count == 1 &&
              diagnosis.DomainEvents.Single()
                  is DiagnosisFailedEvent
                  {
                      Reason: "Analysis could not be completed."
                  }, "Failure must record its reason without generated events.");

        Throws<InvalidOperationException>( () => diagnosis.Fail("Repeated failure."));

        Check(diagnosis.DomainEvents.Count == 1, "Repeated failure must not duplicate events.");
    }
});

// ===== GENERAL CHECKS SUMMARY =====
Console.WriteLine();
Console.WriteLine($"Results: {passed} passed, {failed} failed.");

Environment.ExitCode = failed == 0 ? 0 : 1;

// ===== MYSQL PERSISTENCE CHECKS =====
internal static class DiagnosisPersistenceChecks
{
    public static async Task RunAsync()
    {
        var connection = Environment.GetEnvironmentVariable( "SKYCROP_TEST_CONNECTION");

        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException( "Set SKYCROP_TEST_CONNECTION before running MySQL checks.");

        using var eventServices = DiagnosisEventChecks.CreateServices();
        using var eventScope = eventServices.CreateScope();

        var mediator = eventScope.ServiceProvider.GetRequiredService<IMediator>();

        var recorder = eventScope.ServiceProvider.GetRequiredService<DiagnosisEventRecorder>();

        var options = new DbContextOptionsBuilder<AppDbContext>().UseMySQL(connection)
            .AddInterceptors(new DispatchDomainEventsInterceptor(mediator)).Options;

        await using var context = new AppDbContext(options);

        if (context.Database.GetDbConnection().Database
            != "skycrop_diagnoses_checks")
            throw new InvalidOperationException(
                "These checks require the skycrop_diagnoses_checks database.");

        if ((await context.Database.GetPendingMigrationsAsync()).Any())
            throw new InvalidOperationException( "Apply all migrations before running these checks.");

        await using var transaction = await context.Database.BeginTransactionAsync();

        var passed = 0;

        void Check(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);

            passed++;
            Console.WriteLine($"PASS MySQL: {message}");
        }

        try
        {
            var repository = new DiagnosisRepository(context);
            var unitOfWork = new UnitOfWork(context);
            var queries = new DiagnosisQueryService(repository);
            DiagnosisCommandService Service(IImageAnalysis analysis) => new(
                repository, unitOfWork, analysis, TimeProvider.System,
                NullLogger<DiagnosisCommandService>.Instance);
            var service = Service(new SimulatedImageAnalysis());
            var ids = new List<int>();
            foreach (var scenario in new[] { "HEALTHY", "WATER_STRESS", "PEST" })
            {
                recorder.Events.Clear();
                var result = await service.Handle(new GenerateDiagnosisCommand(scenario));
                Check(result.IsSuccess && result.Value!.Status == EDiagnosisStatus.COMPLETED,
                    $"Standalone {scenario} completes without parcel, flight or images");
                var id = result.Value!.Id;
                ids.Add(id);
                var generatedEvent = recorder.Events.OfType<DiagnosisGeneratedIntegrationEvent>().Single();
                Check(generatedEvent.DiagnosisId == id && generatedEvent.PlotId is null && generatedEvent.FlightId is null,
                    $"Standalone {scenario} publishes persistent IDs and null external references");
                context.ChangeTracker.Clear();
                var loaded = await queries.Handle(new GetDiagnosisByIdQuery(id));
                Check(loaded.IsSuccess && loaded.Value!.PlotId is null && loaded.Value.FlightId is null &&
                      loaded.Value.ImageIds.Count == 0 && loaded.Value.Source == "SIMULATED" &&
                      loaded.Value.CreatedAt.HasValue && loaded.Value.GeneratedAt.HasValue,
                    $"Standalone {scenario} survives a database read with null references");
                Check(loaded.Value!.Anomalies.Count == (scenario == "HEALTHY" ? 0 : 1) &&
                      loaded.Value.Anomalies.All(item => item.Id > 0 && item.Latitude is null && item.Longitude is null),
                    $"Standalone {scenario} preserves anomalies without fabricated coordinates");
            }
            var listing = await queries.Handle(new GetAllDiagnosesQuery());
            Check(listing.IsSuccess && ids.All(id => listing.Value!.Any(item => item.Id == id)),
                "General listing includes standalone diagnoses");
            var ordered = listing.Value!.ToArray();
            Check(ordered.Select(item => item.Id).SequenceEqual(ordered.OrderByDescending(item => item.MonitoredAt)
                    .ThenByDescending(item => item.Id).Select(item => item.Id)),
                "Listing preserves monitoring order and deterministic ties");
            Check(!context.ChangeTracker.Entries<Diagnosis>().Any(), "Queries do not track diagnoses");
            var missing = await queries.Handle(new GetDiagnosisByIdQuery(int.MaxValue));
            Check(missing.IsFailure && missing.DomainError.Code == "Diagnoses.DiagnosisNotFound",
                "Missing diagnosis returns not found");
            var count = await context.Set<Diagnosis>().CountAsync();
            var invalid = await service.Handle(new GenerateDiagnosisCommand("UNKNOWN"));
            Check(invalid.IsFailure && await context.Set<Diagnosis>().CountAsync() == count,
                "Invalid scenarios do not persist a diagnosis");
            var failure = await Service(new FailingImageAnalysis()).Handle(new GenerateDiagnosisCommand("PEST"));
            context.ChangeTracker.Clear();
            var failed = await repository.FindByIdAsNoTrackingAsync(failure.Value!.Id);
            Check(failed!.Status == EDiagnosisStatus.FAILED && failed.FailureReason == "Analysis could not be completed." &&
                  failed.AverageNdvi is null && failed.Anomalies.Count == 0,
                "Provider failure is persisted without exposing internal details");

            // Historical metadata does not require records from other contexts.
            var old = new Diagnosis(1234567, 1234567, new[] { 1234567 }, "HEALTHY",
                DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow);
            old.StartProcessing();
            old.CompleteSimulated("Historical simulated result", new Ndvi(0.8), Array.Empty<Anomaly>(), DateTimeOffset.UtcNow);
            context.Add(old);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            var history = await queries.Handle(new GetDiagnosesByPlotIdQuery(1234567));
            Check(history.IsSuccess && history.Value!.Any(item => item.Id == old.Id),
                "Historical plot filters do not query or require parcel records");
            var facade = new DiagnosesContextFacade(queries);
            var summary = await facade.GetLatestCompletedByPlotIdAsync(1234567);
            Check(summary.IsSuccess && summary.Value!.DiagnosisId == old.Id,
                "Facade preserves historical summaries");
            await DiagnosisEventChecks.RunAsync(context, recorder, 1234567, 1234567, 1234567, Check);
        }
        finally
        {
            await transaction.RollbackAsync();
            Console.WriteLine("Test transaction rolled back.");
        }

        Console.WriteLine($"MySQL results: {passed} passed, 0 failed.");
    }
}

// ===== EVENT PUBLICATION CHECKS AND RECORDERS =====
public sealed class DiagnosisEventRecorder
{
    public ConcurrentQueue<object> Events { get; } = new();
}

public sealed class RecordingDiagnosisEventHandler<T>(
    DiagnosisEventRecorder recorder) : INotificationHandler<T>
    where T : INotification
{
    public Task Handle(T notification, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        recorder.Events.Enqueue(notification);
        return Task.CompletedTask;
    }
}

internal static class DiagnosisEventChecks
{
    public static ServiceProvider CreateServices()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddCortexMediator([typeof(Diagnosis)]);
        services.AddSingleton<DiagnosisEventRecorder>();

        services.AddTransient<
            INotificationHandler<DiagnosisRequestedEvent>,
            RecordingDiagnosisEventHandler<DiagnosisRequestedEvent>>();

        services.AddTransient<
            INotificationHandler<DiagnosisProcessingStartedEvent>,
            RecordingDiagnosisEventHandler<DiagnosisProcessingStartedEvent>>();

        services.AddTransient<
            INotificationHandler<DiagnosisGeneratedIntegrationEvent>,
            RecordingDiagnosisEventHandler<DiagnosisGeneratedIntegrationEvent>>();

        services.AddTransient<
            INotificationHandler<AgronomicAnomalyDetectedIntegrationEvent>,
            RecordingDiagnosisEventHandler<AgronomicAnomalyDetectedIntegrationEvent>>();

        services.AddTransient<
            INotificationHandler<DiagnosisFailedIntegrationEvent>,
            RecordingDiagnosisEventHandler<DiagnosisFailedIntegrationEvent>>();

        return services.BuildServiceProvider();
    }

    public static async Task RunAsync( AppDbContext context, DiagnosisEventRecorder recorder, int plotId,
        int flightId, int imageId, Action<bool, string> check)
    {
        context.ChangeTracker.Clear();
        recorder.Events.Clear();

        var requestedAt = DateTimeOffset.UtcNow;
        var monitoredAt = requestedAt.AddMinutes(-10);

        Diagnosis CreateEventDiagnosis(string scenario) => new(
            plotId, flightId, new[] { imageId }, scenario, monitoredAt, requestedAt);

        var diagnosis = CreateEventDiagnosis("WATER_STRESS");

        context.Add(diagnosis);

        check(recorder.Events.IsEmpty, "Events are not published merely by creating an aggregate");

        await context.SaveChangesAsync();

        var requested = recorder.Events.OfType<DiagnosisRequestedEvent>().Single();

        check(diagnosis.Id > 0 &&
              requested.DiagnosisId == diagnosis.Id &&
              diagnosis.DomainEvents.Count == 0,
            "Request event is dispatched with the database-generated ID");

        recorder.Events.Clear();

        diagnosis.StartProcessing();
        await context.SaveChangesAsync();

        var started = recorder.Events.OfType<DiagnosisProcessingStartedEvent>().Single();

        check(started.DiagnosisId == diagnosis.Id &&
              recorder.Events.Count == 1, "Processing event is dispatched through the EF interceptor");

        recorder.Events.Clear();

        var anomaly = new Anomaly( "WATER_STRESS", "MEDIUM", "Simulated water stress.", "Test sector",
            -12.0464, -77.0428);

        diagnosis.CompleteSimulated( "Simulated analysis completed.", new Ndvi(0.4), new[] { anomaly },
            requestedAt.AddSeconds(1));

        check(recorder.Events.IsEmpty, "Completion is not published before SaveChanges");

        await context.SaveChangesAsync();

        var generated = recorder.Events.OfType<DiagnosisGeneratedIntegrationEvent>().Single();

        var detected = recorder.Events.OfType<AgronomicAnomalyDetectedIntegrationEvent>().Single();

        check(generated.DiagnosisId == diagnosis.Id &&
              generated.PlotId == plotId &&
              generated.MonitoredAt == monitoredAt &&
              generated.AverageNdvi == 0.4 &&
              generated.Source == "SIMULATED" &&
              generated.AnomalyCount == 1 &&
              detected.AnomalyId == anomaly.Id &&
              detected.AnomalyId > 0 &&
              detected.DiagnosisId == diagnosis.Id &&
              recorder.Events.Count == 2, "Saved results reach integration consumers with persistent IDs");

        check(diagnosis.DomainEvents.Count == 0, "Dispatched domain events are cleared");

        await context.SaveChangesAsync();

        check(recorder.Events.Count == 2, "Saving again does not republish the completed events");

        context.ChangeTracker.Clear();
        recorder.Events.Clear();

        var healthy = CreateEventDiagnosis("HEALTHY");

        healthy.StartProcessing();
        context.Add(healthy);
        await context.SaveChangesAsync();

        recorder.Events.Clear();

        healthy.CompleteSimulated( "Healthy simulated result.", new Ndvi(0.8), Array.Empty<Anomaly>(),
            requestedAt.AddSeconds(1));

        await context.SaveChangesAsync();

        check(recorder.Events.Count == 1 &&
              recorder.Events.Single()
                  is DiagnosisGeneratedIntegrationEvent
                  {
                      AnomalyCount: 0, AverageNdvi: 0.8
                  }, "Healthy completion publishes no anomaly integration event");

        context.ChangeTracker.Clear();
        recorder.Events.Clear();

        var failed = CreateEventDiagnosis("WATER_STRESS");

        failed.StartProcessing();
        context.Add(failed);
        await context.SaveChangesAsync();

        recorder.Events.Clear();

        failed.Fail("Analysis could not be completed.");
        await context.SaveChangesAsync();

        check(recorder.Events.Count == 1 &&
              recorder.Events.Single()
                  is DiagnosisFailedIntegrationEvent failure &&
              failure.DiagnosisId == failed.Id &&
              failure.Reason == "Analysis could not be completed.",
            "Failed analysis publishes failure without generated results");
    }
}

// ===== TEST DEPENDENCIES =====
// These implementations exist only in the test project.
// They are never registered in the API.
internal sealed class TestPlotAccessService(int allowedPlotId)
    : IPlotAccessService
{
    public Task<Result> CheckAccessAsync( int plotId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult( plotId == allowedPlotId && allowedPlotId > 0
                ? Result.Success()
                : Result.Failure(Error.Forbidden( "Diagnoses.PlotAccessDenied",
                    "Access to this plot is not allowed.")));
    }

    public Task<Result<IReadOnlyCollection<int>>> GetAccessiblePlotIdsAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyCollection<int> ids = allowedPlotId > 0
            ? new[] { allowedPlotId }
            : Array.Empty<int>();

        return Task.FromResult( Result<IReadOnlyCollection<int>>.Success(ids));
    }
}

internal sealed class FailingImageAnalysis : IImageAnalysis
{
    public Task<ImageAnalysisResult> AnalyzeAsync( ImageAnalysisInput input,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        throw new InvalidOperationException( "Internal test provider failure.");
    }
}

// ===== LOCAL DEMONSTRATION DATA =====
internal static class DiagnosisDemoSeed
{
    public static async Task RunAsync()
    {
        var connection = Environment.GetEnvironmentVariable( "SKYCROP_TEST_CONNECTION");

        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException( "Configure SKYCROP_TEST_CONNECTION.");

        var options = new DbContextOptionsBuilder<AppDbContext>().UseMySQL(connection).Options;

        await using var context = new AppDbContext(options);

        if (context.Database.GetDbConnection().Database
            != "skycrop_diagnoses_checks")
        {
            throw new InvalidOperationException( "Demo data requires skycrop_diagnoses_checks.");
        }

        if ((await context.Database.GetPendingMigrationsAsync()).Any())
            throw new InvalidOperationException( "Apply pending migrations before creating demo data.");

        await using var transaction = await context.Database.BeginTransactionAsync();

        var plot = new AgriculturalPlot(new RegisterPlotCommand( "Diagnoses local demo", "Demo operator",
            "Potato", "Loam", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)), 1m, "Demo north sector",
            -12.0464, -77.0428, null));

        context.Add(plot);
        await context.SaveChangesAsync();

        var drone = new Drone(new CreateDroneCommand( "Diagnosis demo drone", "Demo model",
            $"demo-{Guid.NewGuid():N}", "Demo camera", 30, 100));

        context.Add(drone);
        await context.SaveChangesAsync();

        drone.Update(new UpdateDroneCommand( drone.Id, Status: "AVAILABLE"));

        var route = new FlightRoute(new CreateFlightRouteCommand( "Diagnosis demo route", plot.Id, 30, 4,
            new[]
            {
                new RouteWaypoint(0, -12.0464, -77.0428), new RouteWaypoint(1, -12.0465, -77.0429)
            }));

        context.Add(route);
        await context.SaveChangesAsync();

        var flight = new Flight(drone, route, plot.Id);
        context.Add(flight);
        await context.SaveChangesAsync();

        // Demo fixture only. This does not execute or complete a real flight.
        context.Entry(flight).Property(item => item.Status)
            .CurrentValue = EFlightStatus.COMPLETED;

        context.Entry(flight).Property(item => item.ActiveDroneId)
            .CurrentValue = null;

        var image = new AerialImage( flight.Id, "https://example.com/diagnoses-demo.jpg",
            DateTimeOffset.UtcNow.AddMinutes(-10));

        context.Add(image);
        await context.SaveChangesAsync();

        await transaction.CommitAsync();

        Console.WriteLine("Demo fixtures committed.");
        Console.WriteLine($"Diagnoses__DemoPlotId = {plot.Id}");
        Console.WriteLine("Legacy integration fixtures only; the standalone POST does not require them.");
        Console.WriteLine("Use this standalone POST body:");

        Console.WriteLine(JsonSerializer.Serialize(new
        {
            scenario = "WATER_STRESS"
        }, new JsonSerializerOptions { WriteIndented = true }));
    }
}
