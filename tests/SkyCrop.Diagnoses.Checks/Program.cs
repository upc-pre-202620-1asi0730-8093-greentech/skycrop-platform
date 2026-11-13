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
var requestedAt = new DateTimeOffset(
    2026, 10, 10, 12, 0, 0, TimeSpan.Zero);

Diagnosis CreateDiagnosis(string scenario = "WATER_STRESS") =>
    new(1, 2, new[] { 10, 11 }, scenario,
        requestedAt.AddHours(-1), requestedAt);

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

    throw new Exception(
        $"Expected {typeof(TException).Name}, but no exception was thrown.");
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

// ===== DOMAIN CHECKS =====

Test("NDVI accepts boundary values and compares by value", () =>
{
    foreach (var value in new[] { -1d, 0d, 0.4d, 1d })
        Check(new Ndvi(value).Value == value, "NDVI changed its value.");

    Check(new Ndvi(0.4) == new Ndvi(0.4),
        "Equal NDVI values must be equal.");
});

Test("NDVI rejects out-of-range and non-finite values", () =>
{
    foreach (var value in new[]
             {
                 -1.01, 1.01, double.NaN,
                 double.PositiveInfinity, double.NegativeInfinity
             })
    {
        Throws<ArgumentException>(() => new Ndvi(value));
    }
});

Test("Anomalies support frontend types and severity levels", () =>
{
    foreach (var type in new[]
             {
                 "WATER_STRESS", "PEST",
                 "FERTILIZER_DEFICIENCY", "OTHER"
             })
    {
        foreach (var severity in new[] { "LOW", "MEDIUM", "HIGH", "CRITICAL" })
        {
            var anomaly = new Anomaly(
                type, severity, "Description", "Location");

            Check(anomaly.Type == type && anomaly.Severity == severity,
                "Unexpected anomaly classification.");
        }
    }

    var legacy = CreateAnomaly(" fertilizer_deficiencity ");
    Check(legacy.Type == "FERTILIZER_DEFICIENCY",
        "Legacy spelling was not normalized.");
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
          withoutCoordinates.Longitude is null,
        "Missing coordinates should remain null.");

    var located = new Anomaly(
        "PEST", "HIGH", "Description", "Location", -12.0464, -77.0428);

    Check(located.Latitude == -12.0464 && located.Longitude == -77.0428,
        "Coordinates were not preserved.");
});

Test("Anomalies reject incomplete and invalid coordinates", () =>
{
    Throws<ArgumentException>(() =>
        new Anomaly("PEST", "HIGH", "Description", "Location", 0, null));
    Throws<ArgumentException>(() =>
        new Anomaly("PEST", "HIGH", "Description", "Location", null, 0));
    Throws<ArgumentException>(() =>
        new Anomaly("PEST", "HIGH", "Description", "Location", 91, 0));
    Throws<ArgumentException>(() =>
        new Anomaly("PEST", "HIGH", "Description", "Location", 0, -181));
    Throws<ArgumentException>(() =>
        new Anomaly("PEST", "HIGH", "Description", "Location", double.NaN, 0));
});

Test("A new diagnosis is pending and has no completed results", () =>
{
    var diagnosis = CreateDiagnosis();

    Check(diagnosis.Status == EDiagnosisStatus.PENDING,
        "Initial status must be PENDING.");
    Check(diagnosis.GeneratedAt is null &&
          diagnosis.AverageNdvi is null &&
          diagnosis.Anomalies.Count == 0,
        "Pending diagnosis must not contain completed results.");
    Check(diagnosis.Source == "SIMULATED",
        "Simulation must be identified explicitly.");
});

Test("Diagnosis rejects invalid references and image collections", () =>
{
    Throws<ArgumentException>(() =>
        new Diagnosis(0, 2, new[] { 10 }, "HEALTHY", requestedAt, requestedAt));
    Throws<ArgumentException>(() =>
        new Diagnosis(1, 0, new[] { 10 }, "HEALTHY", requestedAt, requestedAt));

    foreach (var images in new[]
             {
                 Array.Empty<int>(), new[] { 0 },
                 new[] { 10, 10 }, Enumerable.Range(1, 101).ToArray()
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
        new Diagnosis(1, 2, new[] { 10 }, "HEALTHY",
            requestedAt.AddMinutes(1), requestedAt));
});

Test("Diagnosis copies image identifiers from the caller", () =>
{
    var images = new List<int> { 10, 11 };
    var diagnosis = new Diagnosis(
        1, 2, images, "HEALTHY", requestedAt, requestedAt);

    images.Clear();

    Check(diagnosis.ImageIds.SequenceEqual(new[] { 10, 11 }),
        "Changing the input collection changed the diagnosis.");
});

Test("Pending diagnosis cannot complete before processing", () =>
{
    var diagnosis = CreateDiagnosis();

    Throws<InvalidOperationException>(() =>
        diagnosis.CompleteSimulated(
            "Result", new Ndvi(0.4), new[] { CreateAnomaly() }, requestedAt));

    Check(diagnosis.Status == EDiagnosisStatus.PENDING,
        "Invalid completion changed the status.");
});

Test("Water stress diagnosis completes with results and monitoring date", () =>
{
    var diagnosis = CreateDiagnosis();
    var monitoredAt = diagnosis.MonitoredAt;
    var generatedAt = requestedAt.AddMinutes(1);

    diagnosis.StartProcessing();
    diagnosis.CompleteSimulated(
        "Simulated water stress", new Ndvi(0.4),
        new[] { CreateAnomaly() }, generatedAt);

    Check(diagnosis.Status == EDiagnosisStatus.COMPLETED,
        "Diagnosis did not complete.");
    Check(diagnosis.AverageNdvi?.Value == 0.4 &&
          diagnosis.Anomalies.Count == 1 &&
          diagnosis.GeneratedAt == generatedAt,
        "Completed results were not preserved.");
    Check(diagnosis.MonitoredAt == monitoredAt,
        "Completion changed the monitoring date.");

    Throws<InvalidOperationException>(() => diagnosis.StartProcessing());
    Throws<InvalidOperationException>(() => diagnosis.Fail("Late failure"));
    Throws<InvalidOperationException>(() =>
        diagnosis.CompleteSimulated(
            "Replacement", new Ndvi(0.4),
            new[] { CreateAnomaly() }, generatedAt));
});

Test("Healthy diagnosis completes without anomalies", () =>
{
    var diagnosis = CreateDiagnosis("HEALTHY");
    diagnosis.StartProcessing();

    diagnosis.CompleteSimulated(
        "Simulated healthy crop", new Ndvi(0.8),
        Array.Empty<Anomaly>(), requestedAt);

    Check(diagnosis.Status == EDiagnosisStatus.COMPLETED &&
          diagnosis.Anomalies.Count == 0,
        "Healthy result should complete without anomalies.");
});

Test("Rejected results leave processing diagnosis unchanged", () =>
{
    var diagnosis = CreateDiagnosis();
    diagnosis.StartProcessing();

    Throws<ArgumentException>(() =>
        diagnosis.CompleteSimulated(
            "Wrong scenario", new Ndvi(0.55),
            new[] { CreateAnomaly("PEST") }, requestedAt));

    Throws<ArgumentException>(() =>
        diagnosis.CompleteSimulated(
            "Missing anomaly", new Ndvi(0.4),
            Array.Empty<Anomaly>(), requestedAt));

    Throws<ArgumentException>(() =>
        diagnosis.CompleteSimulated(
            "Invalid date", new Ndvi(0.4),
            new[] { CreateAnomaly() }, requestedAt.AddMinutes(-1)));

    Check(diagnosis.Status == EDiagnosisStatus.PROCESSING &&
          diagnosis.AverageNdvi is null &&
          diagnosis.GeneratedAt is null &&
          diagnosis.Anomalies.Count == 0 &&
          diagnosis.Observations == string.Empty,
        "Rejected completion partially modified the diagnosis.");
});

Test("Healthy diagnosis rejects anomalies", () =>
{
    var diagnosis = CreateDiagnosis("HEALTHY");
    diagnosis.StartProcessing();

    Throws<ArgumentException>(() =>
        diagnosis.CompleteSimulated(
            "Invalid healthy result", new Ndvi(0.8),
            new[] { CreateAnomaly() }, requestedAt));
});

Test("Diagnosis rejects duplicate anomaly instances", () =>
{
    var diagnosis = CreateDiagnosis();
    var anomaly = CreateAnomaly();
    diagnosis.StartProcessing();

    Throws<ArgumentException>(() =>
        diagnosis.CompleteSimulated(
            "Duplicate results", new Ndvi(0.4),
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
              diagnosis.Anomalies.Count == 0,
            "Failure must preserve its reason without completed results.");

        Throws<InvalidOperationException>(() => diagnosis.StartProcessing());
        Throws<InvalidOperationException>(() => diagnosis.Fail("Another failure"));
        Throws<InvalidOperationException>(() =>
            diagnosis.CompleteSimulated(
                "Late result", new Ndvi(0.4),
                new[] { CreateAnomaly() }, requestedAt));
    }
});

// ===== SIMULATED ANALYSIS CHECKS =====

var analyzer = new SimulatedImageAnalysis();

ImageAnalysisResult Analyze(string scenario) =>
    analyzer.AnalyzeAsync(
        new ImageAnalysisInput(
            scenario, "North sector", -12.0464, -77.0428))
    .GetAwaiter().GetResult();

var analysisCases = new[]
{
    (Scenario: "HEALTHY", Ndvi: 0.8, Severity: ""),
    (Scenario: "WATER_STRESS", Ndvi: 0.4, Severity: "MEDIUM"),
    (Scenario: "PEST", Ndvi: 0.55, Severity: "HIGH")
};

foreach (var testCase in analysisCases)
{
    Test($"{testCase.Scenario} analysis returns valid results and completes a diagnosis", () =>
    {
        var result = Analyze(testCase.Scenario);
        var expectedAnomalies = testCase.Scenario == "HEALTHY" ? 0 : 1;

        Check(result.AverageNdvi.Value == testCase.Ndvi &&
              result.Anomalies.Count == expectedAnomalies,
            $"Unexpected results for {testCase.Scenario}.");

        if (expectedAnomalies > 0)
        {
            var anomaly = result.Anomalies.Single();

            Check(anomaly.Type == testCase.Scenario &&
                  anomaly.Severity == testCase.Severity &&
                  anomaly.Location == "North sector" &&
                  anomaly.Latitude == -12.0464 &&
                  anomaly.Longitude == -77.0428,
                "Analysis must preserve classification and location.");
        }

        var diagnosis = CreateDiagnosis(testCase.Scenario);
        diagnosis.StartProcessing();
        diagnosis.CompleteSimulated(
            result.Observations,
            result.AverageNdvi,
            result.Anomalies,
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
    var result = analyzer.AnalyzeAsync(
        new ImageAnalysisInput("PEST", "North sector", null, null))
        .GetAwaiter().GetResult();

    var anomaly = result.Anomalies.Single();

    Check(result.AverageNdvi.Value == 0.55 &&
          anomaly.Latitude is null &&
          anomaly.Longitude is null,
        "Missing coordinates must not remove the analysis result.");
});

Test("Analysis rejects unknown scenarios and invalid coordinates", () =>
{
    Throws<ArgumentException>(() => Analyze("UNKNOWN"));

    foreach (var coordinates in new (double? Latitude, double? Longitude)[]
             {
                 (0, null),
                 (null, 0),
                 (91, 0),
                 (0, -181),
                 (double.NaN, 0)
             })
    {
        Throws<ArgumentException>(() =>
            analyzer.AnalyzeAsync(
                new ImageAnalysisInput(
                    "HEALTHY", "North sector",
                    coordinates.Latitude, coordinates.Longitude))
                .GetAwaiter().GetResult());
    }
});

Test("Analysis respects cancellation", () =>
{
    using var cancellation = new CancellationTokenSource();
    cancellation.Cancel();

    Throws<OperationCanceledException>(() =>
        analyzer.AnalyzeAsync(
            new ImageAnalysisInput("HEALTHY", "North sector", null, null),
            cancellation.Token)
        .GetAwaiter().GetResult());
});

// ===== EF MODEL CHECKS =====

// These checks inspect EF metadata and converters.
// They do not open a MySQL connection.
using var diagnosisContext = new AppDbContext(
    new DbContextOptionsBuilder<AppDbContext>()
        .UseMySQL(
            "server=127.0.0.1;database=model_only;user=unused;password=unused;")
        .Options);

Test("EF maps diagnoses and their history index", () =>
{
    var mapping = diagnosisContext.Model.FindEntityType(typeof(Diagnosis))
                  ?? throw new InvalidOperationException(
                      "Diagnosis is missing from the EF model.");

    Check(mapping.GetTableName() == "diagnoses",
        "Unexpected diagnosis table.");

    Check(mapping.GetIndexes().Any(index =>
            index.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { "PlotId", "MonitoredAt", "Id" })),
        "The history index is missing.");

    Check(mapping.FindProperty(nameof(Diagnosis.GeneratedAt))!.IsNullable,
        "Incomplete diagnoses must allow a null generation date.");
});

Test("EF converts NDVI to a number and restores its value object", () =>
{
    var mapping = diagnosisContext.Model.FindEntityType(typeof(Diagnosis))!;
    var converter = mapping.FindProperty(nameof(Diagnosis.AverageNdvi))!
        .GetTypeMapping().Converter!;

    var stored = converter.ConvertToProvider(new Ndvi(0.4));
    var restored = (Ndvi)converter.ConvertFromProvider(stored)!;

    Check(stored is double value && value == 0.4,
        "NDVI must be stored as a number.");
    Check(restored == new Ndvi(0.4),
        "NDVI was not restored correctly.");
    Check(converter.ConvertToProvider(null) is null,
        "Missing NDVI must remain null.");
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

    Check(restored.SequenceEqual(original),
        "Image references changed during conversion.");

    original.Add(12);

    Check(snapshot.SequenceEqual(new[] { 10, 11 }),
        "The change-tracking snapshot shares the original list.");
    Check(!comparer.Equals(original, snapshot),
        "The comparer did not detect a list modification.");
});

Test("EF maps anomalies as owned entities of a diagnosis", () =>
{
    var mapping = diagnosisContext.Model.FindEntityType(typeof(Diagnosis))!;
    var anomaly = mapping.FindNavigation(nameof(Diagnosis.Anomalies))!
        .TargetEntityType;

    Check(anomaly.GetTableName() == "diagnosis_anomalies",
        "Unexpected anomaly table.");

    var ownership = anomaly.GetForeignKeys()
        .Single(foreignKey => foreignKey.IsOwnership);

    Check(ownership.Properties.Single().Name == nameof(Anomaly.DiagnosisId),
        "Anomaly ownership must use DiagnosisId.");

    Check(anomaly.FindProperty(nameof(Anomaly.Latitude))!.IsNullable &&
          anomaly.FindProperty(nameof(Anomaly.Longitude))!.IsNullable,
        "Anomalies without coordinates must remain persistable.");
});

Test("EF normalizes monitoring dates to UTC without changing the instant", () =>
{
    var mapping = diagnosisContext.Model.FindEntityType(typeof(Diagnosis))!;
    var converter = mapping.FindProperty(nameof(Diagnosis.MonitoredAt))!
        .GetTypeMapping().Converter!;

    var localTime = new DateTimeOffset(
        2026, 10, 10, 7, 0, 0, TimeSpan.FromHours(-5));

    var stored = (DateTime)converter.ConvertToProvider(localTime)!;
    var restored = (DateTimeOffset)converter.ConvertFromProvider(stored)!;

    Check(stored == localTime.UtcDateTime,
        "Monitoring date was not converted to UTC.");
    Check(restored == localTime && restored.Offset == TimeSpan.Zero,
        "UTC conversion changed the monitoring instant.");
});

// ===== DEPENDENCY AND REPOSITORY CHECKS =====

Test("Diagnosis dependencies resolve within an application scope", () =>
{
    var services = new ServiceCollection();

    services.AddDbContext<AppDbContext>(options =>
        options.UseMySQL(
            "server=127.0.0.1;database=model_only;user=unused;password=unused;"));

    services.AddDiagnosesContext();

    using var provider = services.BuildServiceProvider(
        new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });

    using var scope = provider.CreateScope();

    var repository = scope.ServiceProvider
        .GetRequiredService<IDiagnosisRepository>();

    var analysis = scope.ServiceProvider
        .GetRequiredService<IImageAnalysis>();

    Check(repository is DiagnosisRepository,
        "The diagnosis repository was not registered correctly.");

    Check(analysis is SimulatedImageAnalysis,
        "The simulated analysis service was not registered correctly.");

    Check(ReferenceEquals(
            repository,
            scope.ServiceProvider.GetRequiredService<IDiagnosisRepository>()),
        "A scope must reuse its repository instance.");
});

Test("Repository stages a completed diagnosis and its anomalies", () =>
{
    using var context = new AppDbContext(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(
                "server=127.0.0.1;database=model_only;user=unused;password=unused;")
            .Options);

    var repository = new DiagnosisRepository(context);
    var diagnosis = CreateDiagnosis();
    var result = Analyze("WATER_STRESS");

    diagnosis.StartProcessing();
    diagnosis.CompleteSimulated(
        result.Observations,
        result.AverageNdvi,
        result.Anomalies,
        requestedAt.AddMinutes(1));

    repository.AddAsync(diagnosis).GetAwaiter().GetResult();

    Check(context.Entry(diagnosis).State == EntityState.Added,
        "The diagnosis was not staged for insertion.");

    var anomalyEntries = context.ChangeTracker.Entries<Anomaly>().ToArray();

    Check(anomalyEntries.Length == 1 &&
          anomalyEntries[0].State == EntityState.Added,
        "The owned anomaly was not staged with its diagnosis.");

    // No SaveChanges call: this test does not write to MySQL.
});

Test("Repository rejects invalid plot identifiers before querying MySQL", () =>
{
    using var context = new AppDbContext(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(
                "server=127.0.0.1;database=model_only;user=unused;password=unused;")
            .Options);

    var repository = new DiagnosisRepository(context);

    foreach (var plotId in new[] { 0, -1 })
    {
        Throws<ArgumentException>(() =>
            repository.FindByPlotIdAsync(plotId).GetAwaiter().GetResult());
    }
});

// ===== ACCESS CHECKS =====

Test("Unconfigured access never grants permission to a plot", () =>
{
    var services = new ServiceCollection();

    services.AddDbContext<AppDbContext>(options =>
        options.UseMySQL(
            "server=127.0.0.1;database=model_only;user=unused;password=unused;"));

    services.AddDiagnosesContext();

    using var provider = services.BuildServiceProvider(
        new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });

    using var scope = provider.CreateScope();

    var access = scope.ServiceProvider
        .GetRequiredService<IPlotAccessService>();

    var result = access.CheckAccessAsync(1).GetAwaiter().GetResult();

    Check(result.IsFailure &&
          result.DomainError.Code == "Diagnoses.AccessNotConfigured",
        "Missing IAM integration must not grant access.");
});

Test("Unconfigured access does not disguise failure as an empty history", () =>
{
    IPlotAccessService access = new UnconfiguredPlotAccessService();

    var result = access.GetAccessiblePlotIdsAsync()
        .GetAwaiter().GetResult();

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
        var result = access.CheckAccessAsync(plotId)
            .GetAwaiter().GetResult();

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
        access.CheckAccessAsync(1, cancellation.Token)
            .GetAwaiter().GetResult());

    Throws<OperationCanceledException>(() =>
        access.GetAccessiblePlotIdsAsync(cancellation.Token)
            .GetAwaiter().GetResult());
});

Test("Local demo grants access only to its configured plot", () =>
{
    var http = new DefaultHttpContext();
    http.Connection.RemoteIpAddress = IPAddress.Loopback;

    var access = new LocalDemoPlotAccessService(
        7, new HttpContextAccessor { HttpContext = http });

    var allowed = access.CheckAccessAsync(7).GetAwaiter().GetResult();
    var denied = access.CheckAccessAsync(8).GetAwaiter().GetResult();

    Check(allowed.IsSuccess && denied.IsFailure,
        "Local demo permissions must be limited to the configured plot.");
});

Test("Local demo rejects non-local requests", () =>
{
    var http = new DefaultHttpContext();
    http.Connection.RemoteIpAddress = IPAddress.Parse("192.0.2.10");

    var access = new LocalDemoPlotAccessService(
        7, new HttpContextAccessor { HttpContext = http });

    var detail = access.CheckAccessAsync(7).GetAwaiter().GetResult();
    var list = access.GetAccessiblePlotIdsAsync().GetAwaiter().GetResult();

    Check(detail.IsFailure && list.IsFailure &&
          detail.DomainError.Code == "Diagnoses.LocalDemoOnly",
        "Remote requests must not obtain demonstration permissions.");
});

Test("Local demo rejects calls without an HTTP request", () =>
{
    var access = new LocalDemoPlotAccessService(
        7, new HttpContextAccessor());

    var result = access.CheckAccessAsync(7).GetAwaiter().GetResult();

    Check(result.IsFailure,
        "Missing HTTP context must not grant demonstration access.");
});

// ===== MAP CHECKS =====

Test("Map preserves located results and provides their legend", () =>
{
    var diagnosis = CreateDiagnosis();
    diagnosis.StartProcessing();

    diagnosis.CompleteSimulated(
        "Simulated water stress.",
        new Ndvi(0.4),
        [
            new Anomaly(
                "WATER_STRESS", "MEDIUM",
                "Simulated water stress.", "North sector",
                -12.0464, -77.0428)
        ],
        requestedAt.AddMinutes(1));

    var map = DiagnosisMapResourceFromEntityAssembler
        .ToResourceFromEntity(diagnosis);

    Check(map.Status == "AVAILABLE" &&
          map.UnavailableReason is null &&
          map.Source == "SIMULATED",
        "Located simulated results must produce an available map.");

    Check(map.Points.Count == 1 &&
          map.Points.Single().Latitude == -12.0464 &&
          map.Points.Single().Longitude == -77.0428,
        "Map coordinates must preserve the diagnosis results.");

    Check(map.Legend.Any(item =>
            item.Severity == map.Points.Single().Severity &&
            item.Color == "#ac7200"),
        "The represented severity must have its frontend legend color.");
});

Test("Map never fabricates missing coordinates", () =>
{
    var diagnosis = CreateDiagnosis();
    diagnosis.StartProcessing();

    diagnosis.CompleteSimulated(
        "Water stress without coordinates.",
        new Ndvi(0.4),
        [CreateAnomaly()],
        requestedAt.AddMinutes(1));

    var map = DiagnosisMapResourceFromEntityAssembler
        .ToResourceFromEntity(diagnosis);

    Check(map.Status == "UNAVAILABLE" &&
          map.UnavailableReason == "MISSING_COORDINATES" &&
          map.Points.Count == 0 &&
          map.UnlocatedAnomalies == 1,
        "Results without coordinates must not become map points.");

    Check(diagnosis.Status == EDiagnosisStatus.COMPLETED &&
          diagnosis.Anomalies.Count == 1 &&
          diagnosis.AverageNdvi!.Value == 0.4,
        "Unavailable map data must not alter the completed diagnosis.");
});

Test("Partial map preserves unlocated anomalies in the diagnosis", () =>
{
    var diagnosis = CreateDiagnosis();
    diagnosis.StartProcessing();

    diagnosis.CompleteSimulated(
        "Results with mixed coordinate availability.",
        new Ndvi(0.4),
        [
            new Anomaly(
                "WATER_STRESS", "MEDIUM",
                "Located result.", "North sector",
                -12.0464, -77.0428),
            CreateAnomaly()
        ],
        requestedAt.AddMinutes(1));

    var map = DiagnosisMapResourceFromEntityAssembler
        .ToResourceFromEntity(diagnosis);

    Check(map.Status == "PARTIAL" &&
          map.TotalAnomalies == 2 &&
          map.UnlocatedAnomalies == 1 &&
          map.Points.Count == 1,
        "Partial maps must report the results they cannot locate.");

    Check(diagnosis.Anomalies.Count == 2,
        "Generating map data must preserve all diagnosis anomalies.");
});

Test("Healthy diagnosis does not invent risk points", () =>
{
    var diagnosis = CreateDiagnosis("HEALTHY");
    diagnosis.StartProcessing();

    diagnosis.CompleteSimulated(
        "Healthy simulated result.",
        new Ndvi(0.8),
        Array.Empty<Anomaly>(),
        requestedAt.AddMinutes(1));

    var map = DiagnosisMapResourceFromEntityAssembler
        .ToResourceFromEntity(diagnosis);

    Check(map.Status == "UNAVAILABLE" &&
          map.UnavailableReason == "NO_ANOMALIES" &&
          map.Points.Count == 0 &&
          map.TotalAnomalies == 0,
        "Healthy results must not generate artificial risk markers.");

    Check(diagnosis.AverageNdvi!.Value == 0.8,
        "Healthy results must remain available without map points.");
});

Test("Unfinished and failed diagnoses do not expose completed map data", () =>
{
    var diagnosis = CreateDiagnosis();

    void CheckUnavailable()
    {
        var map = DiagnosisMapResourceFromEntityAssembler
            .ToResourceFromEntity(diagnosis);

        Check(map.Status == "UNAVAILABLE" &&
              map.UnavailableReason == "DIAGNOSIS_NOT_COMPLETED" &&
              map.Points.Count == 0,
            "Only completed diagnoses may expose map results.");
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

    var requested = diagnosis.DomainEvents
        .OfType<DiagnosisRequestedEvent>()
        .Single();

    Check(requested.PlotId == diagnosis.PlotId &&
          requested.FlightId == diagnosis.FlightId &&
          requested.RequestedAt == requestedAt,
        "Request event must identify the monitoring request.");

    diagnosis.ClearDomainEvents();
    diagnosis.StartProcessing();

    Check(diagnosis.DomainEvents.Count == 1 &&
          diagnosis.DomainEvents.Single()
              is DiagnosisProcessingStartedEvent,
        "Starting processing must record one event.");

    Throws<InvalidOperationException>(
        () => diagnosis.StartProcessing());

    Check(diagnosis.DomainEvents.Count == 1,
        "Rejected transitions must not add another event.");
});

Test("Completed diagnosis records generated and anomaly facts", () =>
{
    var diagnosis = CreateDiagnosis();
    diagnosis.StartProcessing();
    diagnosis.ClearDomainEvents();

    diagnosis.CompleteSimulated(
        "Simulated water stress.",
        new Ndvi(0.4),
        [CreateAnomaly()],
        requestedAt.AddMinutes(1));

    var generated = diagnosis.DomainEvents
        .OfType<DiagnosisGeneratedEvent>()
        .Single();

    var detected = diagnosis.DomainEvents
        .OfType<AgronomicAnomalyDetectedEvent>()
        .Single();

    Check(generated.MonitoredAt == diagnosis.MonitoredAt &&
          generated.GeneratedAt == requestedAt.AddMinutes(1) &&
          generated.AverageNdvi == 0.4 &&
          generated.AnomalyCount == 1 &&
          generated.Source == "SIMULATED",
        "Generated event must preserve monitoring and result data.");

    Check(detected.Type == "WATER_STRESS" &&
          detected.Severity == "MEDIUM" &&
          detected.Source == "SIMULATED",
        "Anomaly event must preserve its classification and source.");

    Check(diagnosis.DomainEvents.Count == 2,
        "One anomaly must produce two completion-related events.");

    Throws<InvalidOperationException>(() =>
        diagnosis.CompleteSimulated(
            "Repeated completion.",
            new Ndvi(0.4),
            [CreateAnomaly()],
            requestedAt.AddMinutes(2)));

    Check(diagnosis.DomainEvents.Count == 2,
        "Repeated completion must not duplicate events.");

    diagnosis.ClearDomainEvents();

    Check(diagnosis.DomainEvents.Count == 0,
        "Published events must be removable from the aggregate.");
});

Test("Healthy completion does not record anomaly events", () =>
{
    var diagnosis = CreateDiagnosis("HEALTHY");
    diagnosis.StartProcessing();
    diagnosis.ClearDomainEvents();

    diagnosis.CompleteSimulated(
        "Healthy simulated result.",
        new Ndvi(0.8),
        Array.Empty<Anomaly>(),
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
        diagnosis.CompleteSimulated(
            "Invalid scenario results.",
            new Ndvi(0.4),
            [CreateAnomaly("PEST")],
            requestedAt.AddMinutes(1)));

    Check(diagnosis.Status == EDiagnosisStatus.PROCESSING &&
          diagnosis.DomainEvents.Count == 0,
        "Rejected results must preserve state without success events.");
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
                  },
            "Failure must record its reason without generated events.");

        Throws<InvalidOperationException>(
            () => diagnosis.Fail("Repeated failure."));

        Check(diagnosis.DomainEvents.Count == 1,
            "Repeated failure must not duplicate events.");
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
        var connection = Environment.GetEnvironmentVariable(
            "SKYCROP_TEST_CONNECTION");

        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException(
                "Set SKYCROP_TEST_CONNECTION before running MySQL checks.");

        using var eventServices = DiagnosisEventChecks.CreateServices();
        using var eventScope = eventServices.CreateScope();

        var mediator = eventScope.ServiceProvider
            .GetRequiredService<IMediator>();

        var recorder = eventScope.ServiceProvider
            .GetRequiredService<DiagnosisEventRecorder>();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(connection)
            .AddInterceptors(new DispatchDomainEventsInterceptor(mediator))
            .Options;

        await using var context = new AppDbContext(options);

        if (context.Database.GetDbConnection().Database
            != "skycrop_diagnoses_checks")
            throw new InvalidOperationException(
                "These checks require the skycrop_diagnoses_checks database.");

        if ((await context.Database.GetPendingMigrationsAsync()).Any())
            throw new InvalidOperationException(
                "Apply all migrations before running these checks.");

        await using var transaction =
            await context.Database.BeginTransactionAsync();

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
            var plot = new AgriculturalPlot(new RegisterPlotCommand(
                "Diagnosis test plot",
                "Test operator",
                "Potato",
                "Loam",
                new DateOnly(2026, 1, 1),
                1m,
                "Test sector",
                -12.0464,
                -77.0428,
                null));

            context.Add(plot);
            await context.SaveChangesAsync();

            var drone = new Drone(new CreateDroneCommand(
                "Diagnosis test drone",
                "Test model",
                $"diag-{Guid.NewGuid():N}",
                "Test camera",
                30,
                100));

            context.Add(drone);
            await context.SaveChangesAsync();

            drone.Update(new UpdateDroneCommand(
                drone.Id, Status: "AVAILABLE"));

            var route = new FlightRoute(new CreateFlightRouteCommand(
                "Diagnosis test route",
                plot.Id,
                30,
                4,
                new[]
                {
                    new RouteWaypoint(0, -12.0464, -77.0428),
                    new RouteWaypoint(1, -12.0465, -77.0429)
                }));

            context.Add(route);
            await context.SaveChangesAsync();

            var flight = new Flight(drone, route, plot.Id);
            context.Add(flight);
            await context.SaveChangesAsync();

            var monitoredAt = new DateTimeOffset(
                2026, 10, 10, 12, 0, 0, TimeSpan.Zero);

            var image = new AerialImage(
                flight.Id,
                "https://example.com/diagnosis-test.jpg",
                monitoredAt);

            context.Add(image);
            await context.SaveChangesAsync();

            var repository = new DiagnosisRepository(context);
            var unitOfWork = new UnitOfWork(context);
            var analyzer = new SimulatedImageAnalysis();

            var recent = new Diagnosis(
                plot.Id, flight.Id, new[] { image.Id },
                "WATER_STRESS", monitoredAt, monitoredAt.AddHours(1));

            recent.StartProcessing();

            var result = await analyzer.AnalyzeAsync(
                new ImageAnalysisInput(
                    "WATER_STRESS", "Test sector", -12.0464, -77.0428));

            recent.CompleteSimulated(
                result.Observations,
                result.AverageNdvi,
                result.Anomalies,
                monitoredAt.AddHours(2));

            await repository.AddAsync(recent);
            await unitOfWork.CompleteAsync();

            var diagnosisId = recent.Id;
            var plotId = plot.Id;
            var imageId = image.Id;
            var flightId = flight.Id;

            Check(diagnosisId > 0,
                "MySQL generates a diagnosis identifier");

            // Force the following query to read from the database.
            context.ChangeTracker.Clear();

            var loaded = await repository.FindByIdAsNoTrackingAsync(diagnosisId)
                         ?? throw new InvalidOperationException(
                             "The saved diagnosis was not found.");

            Check(loaded.Status == EDiagnosisStatus.COMPLETED &&
                  loaded.AverageNdvi?.Value == 0.4 &&
                  loaded.Source == "SIMULATED" &&
                  loaded.ImageIds.SequenceEqual(new[] { imageId }),
                "Diagnosis results and image references survive a database read");

            var anomaly = loaded.Anomalies.Single();

            Check(anomaly.Id > 0 &&
                  anomaly.DiagnosisId == diagnosisId &&
                  anomaly.Type == "WATER_STRESS" &&
                  anomaly.Latitude == -12.0464 &&
                  anomaly.Longitude == -77.0428,
                "Owned anomalies are saved and loaded with their diagnosis");

            Check(loaded.MonitoredAt == monitoredAt &&
                  loaded.MonitoredAt.Offset == TimeSpan.Zero &&
                  loaded.CreatedAt.HasValue &&
                  loaded.UpdatedAt.HasValue,
                "Monitoring and audit timestamps are preserved");

            var older = new Diagnosis(
                plotId, flightId, new[] { imageId },
                "HEALTHY", monitoredAt.AddDays(-1), monitoredAt.AddHours(3));

            older.Fail("Simulated processing failure");

            await repository.AddAsync(older);
            await unitOfWork.CompleteAsync();

            context.ChangeTracker.Clear();

            var history = (await repository.FindByPlotIdAsync(plotId)).ToArray();

            Check(history.Length == 2 &&
                  history[0].Id == diagnosisId &&
                  history[1].Id == older.Id,
                "History follows monitoring time rather than insertion order");

            var failed = history[1];

            Check(failed.Status == EDiagnosisStatus.FAILED &&
                  failed.FailureReason == "Simulated processing failure" &&
                  failed.AverageNdvi is null &&
                  failed.GeneratedAt is null &&
                  failed.Anomalies.Count == 0,
                "Failed diagnoses preserve their reason without completed results");

            Check(!context.ChangeTracker.Entries<Diagnosis>().Any(),
                "History queries do not track diagnosis entities");
            var monitoring = new MonitoringDataSource(context);

            var invalidImages = await monitoring.GetAsync(
                plotId, flightId, Array.Empty<int>());

            Check(invalidImages.IsFailure &&
                  invalidImages.DomainError.Code == "Diagnoses.InvalidImages",
                "Monitoring rejects empty image selections");

            var plannedFlight = await monitoring.GetAsync(
                plotId, flightId, new[] { imageId });

            Check(plannedFlight.IsFailure &&
                  plannedFlight.DomainError.Code == "Diagnoses.FlightNotCompleted",
                "Monitoring rejects flights that are not completed");

// Test fixture only: represent a flight completed by the future
// Drones capture integration. This is not a production operation.
            var storedFlight = await context.Set<Flight>()
                .SingleAsync(item => item.Id == flightId);

            context.Entry(storedFlight)
                .Property(item => item.Status)
                .CurrentValue = EFlightStatus.COMPLETED;

            context.Entry(storedFlight)
                .Property(item => item.ActiveDroneId)
                .CurrentValue = null;

            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var monitoringResult = await monitoring.GetAsync(
                plotId, flightId, new[] { imageId });

            Check(monitoringResult.IsSuccess &&
                  monitoringResult.Value is { } data &&
                  data.PlotId == plotId &&
                  data.FlightId == flightId &&
                  data.ImageIds.SequenceEqual(new[] { imageId }) &&
                  data.MonitoredAt == monitoredAt &&
                  data.Latitude == -12.0464 &&
                  data.Longitude == -77.0428,
                "Monitoring returns verified references, capture date and coordinates");

            var missingImage = await monitoring.GetAsync(
                plotId, flightId, new[] { int.MaxValue });

            Check(missingImage.IsFailure &&
                  missingImage.DomainError.Code == "Diagnoses.ImagesUnavailable",
                "Monitoring rejects unavailable image references");

            DiagnosisCommandService CreateCommandService(
                IPlotAccessService plotAccess,
                IImageAnalysis imageAnalysis)
            {
                return new DiagnosisCommandService(
                    new DiagnosisRepository(context),
                    new UnitOfWork(context),
                    plotAccess,
                    new MonitoringDataSource(context),
                    imageAnalysis,
                    TimeProvider.System,
                    NullLogger<DiagnosisCommandService>.Instance);
            }

            var command = new GenerateDiagnosisCommand(
                plotId, flightId, new[] { imageId }, "WATER_STRESS");

            var countBefore = await context.Set<Diagnosis>().CountAsync();

            var deniedService = CreateCommandService(
                new TestPlotAccessService(-1),
                new SimulatedImageAnalysis());

            var deniedResult = await deniedService.Handle(command);

            Check(deniedResult.IsFailure &&
                  deniedResult.DomainError.Code == "Diagnoses.PlotAccessDenied" &&
                  await context.Set<Diagnosis>().CountAsync() == countBefore,
                "Generation without permission does not create a diagnosis");

            var commandService = CreateCommandService(
                new TestPlotAccessService(plotId),
                new SimulatedImageAnalysis());

            var generatedResult = await commandService.Handle(command);

            if (generatedResult.IsFailure || generatedResult.Value is null)
                throw new InvalidOperationException(
                    $"Generation failed: {generatedResult.DomainError.Code}");

            var generatedId = generatedResult.Value.Id;

            context.ChangeTracker.Clear();

            var generated = await new DiagnosisRepository(context)
                .FindByIdAsNoTrackingAsync(generatedId);

            Check(generated is not null &&
                  generated.Status == EDiagnosisStatus.COMPLETED &&
                  generated.AverageNdvi?.Value == 0.4 &&
                  generated.Anomalies.Count == 1 &&
                  generated.MonitoredAt == monitoredAt,
                "Generation service persists a complete diagnosis from verified monitoring");

            var failingService = CreateCommandService(
                new TestPlotAccessService(plotId),
                new FailingImageAnalysis());

            var failureResult = await failingService.Handle(command);

            if (failureResult.IsFailure || failureResult.Value is null)
                throw new InvalidOperationException(
                    "An analysis failure must preserve a queryable diagnosis.");

            var failedId = failureResult.Value.Id;

            context.ChangeTracker.Clear();

            var savedFailure = await new DiagnosisRepository(context)
                .FindByIdAsNoTrackingAsync(failedId);

            Check(savedFailure is not null &&
                  savedFailure.Status == EDiagnosisStatus.FAILED &&
                  savedFailure.AverageNdvi is null &&
                  savedFailure.Anomalies.Count == 0 &&
                  savedFailure.FailureReason == "Analysis could not be completed.",
                "Provider failure is persisted without leaking internal exception details");

            var countBeforeInvalid = await context.Set<Diagnosis>().CountAsync();

            var invalidResult = await commandService.Handle(
                command with { Scenario = "UNKNOWN" });

            Check(invalidResult.IsFailure &&
                  invalidResult.DomainError.Code == "Diagnoses.InvalidRequest" &&
                  await context.Set<Diagnosis>().CountAsync() == countBeforeInvalid,
                "Invalid generation requests do not create database records");
            context.ChangeTracker.Clear();

            var queries = new DiagnosisQueryService(
                new DiagnosisRepository(context),
                new TestPlotAccessService(plotId));

            var detailResult = await queries.Handle(
                new GetDiagnosisByIdQuery(generatedId));

            Check(detailResult.IsSuccess &&
                  detailResult.Value is { } detail &&
                  detail.Id == generatedId &&
                  detail.Anomalies.Count == 1,
                "Authorized detail queries return the diagnosis and its anomalies");

            var historyResult = await queries.Handle(
                new GetDiagnosesByPlotIdQuery(plotId));

            if (historyResult.IsFailure || historyResult.Value is null)
                throw new InvalidOperationException("Authorized history query failed.");

            var orderedHistory = historyResult.Value.ToArray();

            Check(orderedHistory.Length == 4 &&
                  orderedHistory.Select(item => item.Id).SequenceEqual(
                      orderedHistory
                          .OrderByDescending(item => item.MonitoredAt)
                          .ThenByDescending(item => item.Id)
                          .Select(item => item.Id)),
                "History queries preserve monitoring order and deterministic ties");

            var listResult = await queries.Handle(new GetAllDiagnosesQuery());

            Check(listResult.IsSuccess &&
                  listResult.Value is { } accessible &&
                  accessible.Count() == 4 &&
                  accessible.All(item => item.PlotId == plotId),
                "General listing returns diagnoses from authorized plots");

// A different existing plot with no diagnoses.
            var emptyPlot = new AgriculturalPlot(new RegisterPlotCommand(
                "Empty diagnosis test plot",
                "Test operator",
                "Potato",
                "Loam",
                new DateOnly(2026, 1, 1),
                1m,
                "Other sector",
                null,
                null,
                null));

            context.Add(emptyPlot);
            await context.SaveChangesAsync();

            var restrictedQueries = new DiagnosisQueryService(
                new DiagnosisRepository(context),
                new TestPlotAccessService(emptyPlot.Id));

            var restrictedList = await restrictedQueries.Handle(
                new GetAllDiagnosesQuery());

            Check(restrictedList.IsSuccess &&
                  restrictedList.Value is { } restrictedItems &&
                  !restrictedItems.Any(),
                "Listing an authorized empty plot does not expose another plot's diagnoses");

            var forbiddenHistory = await restrictedQueries.Handle(
                new GetDiagnosesByPlotIdQuery(plotId));

            Check(forbiddenHistory.IsFailure &&
                  forbiddenHistory.DomainError.Code == "Diagnoses.PlotAccessDenied",
                "History queries reject access to another plot");

            var hiddenDetail = await restrictedQueries.Handle(
                new GetDiagnosisByIdQuery(generatedId));

            var nonexistentDetail = await restrictedQueries.Handle(
                new GetDiagnosisByIdQuery(int.MaxValue));

            Check(hiddenDetail.IsFailure &&
                  nonexistentDetail.IsFailure &&
                  hiddenDetail.DomainError.Code == "Diagnoses.DiagnosisNotFound" &&
                  hiddenDetail.DomainError == nonexistentDetail.DomainError,
                "Inaccessible and nonexistent diagnoses produce the same detail error");

            var emptyHistory = await restrictedQueries.Handle(
                new GetDiagnosesByPlotIdQuery(emptyPlot.Id));

            Check(emptyHistory.IsSuccess &&
                  emptyHistory.Value is { } emptyItems &&
                  !emptyItems.Any(),
                "An authorized plot without diagnoses returns a successful empty history");

            var unconfiguredQueries = new DiagnosisQueryService(
                new DiagnosisRepository(context),
                new UnconfiguredPlotAccessService());

            var unavailableAccess = await unconfiguredQueries.Handle(
                new GetAllDiagnosesQuery());

            Check(unavailableAccess.IsFailure &&
                  unavailableAccess.DomainError.Code == "Diagnoses.AccessNotConfigured",
                "Queries preserve missing access integration errors");
                        var facade = new DiagnosesContextFacade(queries);

            var latestSummary = await facade
                .GetLatestCompletedByPlotIdAsync(plotId);

            Check(latestSummary.IsSuccess &&
                  latestSummary.Value is { } summary &&
                  summary.DiagnosisId == generatedId &&
                  summary.MonitoredAt == monitoredAt &&
                  summary.AverageNdvi == 0.4 &&
                  summary.Source == "SIMULATED",
                "Facade returns the latest completed diagnosis summary");

            // A completed diagnosis inserted later with older monitoring
            // data must not replace the current result.
            var lateOlderDiagnosis = new Diagnosis(
                plotId,
                flightId,
                new[] { imageId },
                "HEALTHY",
                monitoredAt.AddDays(-1),
                DateTimeOffset.UtcNow);

            lateOlderDiagnosis.StartProcessing();
            lateOlderDiagnosis.CompleteSimulated(
                "Older healthy monitoring result.",
                new Ndvi(0.8),
                Array.Empty<GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Entities.Anomaly>(),
                DateTimeOffset.UtcNow);

            context.Add(lateOlderDiagnosis);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var afterOlderResult = await facade
                .GetLatestCompletedByPlotIdAsync(plotId);

            Check(afterOlderResult.IsSuccess &&
                  afterOlderResult.Value?.DiagnosisId == generatedId,
                "Older monitoring does not replace the latest completed result");

            // More recent monitoring without successful analysis
            // must not replace completed results.
            var unfinishedDiagnosis = new Diagnosis(
                plotId,
                flightId,
                new[] { imageId },
                "WATER_STRESS",
                DateTimeOffset.UtcNow.AddMinutes(-1),
                DateTimeOffset.UtcNow);

            context.Add(unfinishedDiagnosis);
            await context.SaveChangesAsync();

            var whilePending = await facade
                .GetLatestCompletedByPlotIdAsync(plotId);

            Check(whilePending.IsSuccess &&
                  whilePending.Value?.DiagnosisId == generatedId,
                "Pending diagnoses do not replace completed results");

            unfinishedDiagnosis.Fail("Simulated failure.");
            await context.SaveChangesAsync();

            var afterFailure = await facade
                .GetLatestCompletedByPlotIdAsync(plotId);

            Check(afterFailure.IsSuccess &&
                  afterFailure.Value?.DiagnosisId == generatedId,
                "Failed diagnoses do not replace completed results");

            var restrictedFacade =
                new DiagnosesContextFacade(restrictedQueries);

            var emptySummary = await restrictedFacade
                .GetLatestCompletedByPlotIdAsync(emptyPlot.Id);

            Check(emptySummary.IsSuccess && emptySummary.Value is null,
                "A plot without completed diagnoses returns an empty summary");

            var deniedSummary = await restrictedFacade
                .GetLatestCompletedByPlotIdAsync(plotId);

            Check(deniedSummary.IsFailure &&
                  deniedSummary.DomainError.Code ==
                      "Diagnoses.PlotAccessDenied",
                "Facade preserves plot access restrictions");
            await DiagnosisEventChecks.RunAsync(
                context,
                recorder,
                plotId,
                flightId,
                imageId,
                Check);
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

    public static async Task RunAsync(
        AppDbContext context,
        DiagnosisEventRecorder recorder,
        int plotId,
        int flightId,
        int imageId,
        Action<bool, string> check)
    {
        context.ChangeTracker.Clear();
        recorder.Events.Clear();

        var requestedAt = DateTimeOffset.UtcNow;
        var monitoredAt = requestedAt.AddMinutes(-10);

        var diagnosis = new Diagnosis(
            plotId,
            flightId,
            new[] { imageId },
            "WATER_STRESS",
            monitoredAt,
            requestedAt);

        context.Add(diagnosis);

        check(recorder.Events.IsEmpty,
            "Events are not published merely by creating an aggregate");

        await context.SaveChangesAsync();

        var requested = recorder.Events
            .OfType<DiagnosisRequestedEvent>()
            .Single();

        check(diagnosis.Id > 0 &&
              requested.DiagnosisId == diagnosis.Id &&
              diagnosis.DomainEvents.Count == 0,
            "Request event is dispatched with the database-generated ID");

        recorder.Events.Clear();

        diagnosis.StartProcessing();
        await context.SaveChangesAsync();

        var started = recorder.Events
            .OfType<DiagnosisProcessingStartedEvent>()
            .Single();

        check(started.DiagnosisId == diagnosis.Id &&
              recorder.Events.Count == 1,
            "Processing event is dispatched through the EF interceptor");

        recorder.Events.Clear();

        var anomaly = new Anomaly(
            "WATER_STRESS",
            "MEDIUM",
            "Simulated water stress.",
            "Test sector",
            -12.0464,
            -77.0428);

        diagnosis.CompleteSimulated(
            "Simulated analysis completed.",
            new Ndvi(0.4),
            new[] { anomaly },
            requestedAt.AddSeconds(1));

        check(recorder.Events.IsEmpty,
            "Completion is not published before SaveChanges");

        await context.SaveChangesAsync();

        var generated = recorder.Events
            .OfType<DiagnosisGeneratedIntegrationEvent>()
            .Single();

        var detected = recorder.Events
            .OfType<AgronomicAnomalyDetectedIntegrationEvent>()
            .Single();

        check(generated.DiagnosisId == diagnosis.Id &&
              generated.PlotId == plotId &&
              generated.MonitoredAt == monitoredAt &&
              generated.AverageNdvi == 0.4 &&
              generated.Source == "SIMULATED" &&
              generated.AnomalyCount == 1 &&
              detected.AnomalyId == anomaly.Id &&
              detected.AnomalyId > 0 &&
              detected.DiagnosisId == diagnosis.Id &&
              recorder.Events.Count == 2,
            "Saved results reach integration consumers with persistent IDs");

        check(diagnosis.DomainEvents.Count == 0,
            "Dispatched domain events are cleared");

        await context.SaveChangesAsync();

        check(recorder.Events.Count == 2,
            "Saving again does not republish the completed events");

        context.ChangeTracker.Clear();
        recorder.Events.Clear();

        var healthy = new Diagnosis(
            plotId,
            flightId,
            new[] { imageId },
            "HEALTHY",
            monitoredAt,
            requestedAt);

        healthy.StartProcessing();
        context.Add(healthy);
        await context.SaveChangesAsync();

        recorder.Events.Clear();

        healthy.CompleteSimulated(
            "Healthy simulated result.",
            new Ndvi(0.8),
            Array.Empty<Anomaly>(),
            requestedAt.AddSeconds(1));

        await context.SaveChangesAsync();

        check(recorder.Events.Count == 1 &&
              recorder.Events.Single()
                  is DiagnosisGeneratedIntegrationEvent
                  {
                      AnomalyCount: 0,
                      AverageNdvi: 0.8
                  },
            "Healthy completion publishes no anomaly integration event");

        context.ChangeTracker.Clear();
        recorder.Events.Clear();

        var failed = new Diagnosis(
            plotId,
            flightId,
            new[] { imageId },
            "WATER_STRESS",
            monitoredAt,
            requestedAt);

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
    public Task<Result> CheckAccessAsync(
        int plotId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(
            plotId == allowedPlotId && allowedPlotId > 0
                ? Result.Success()
                : Result.Failure(Error.Forbidden(
                    "Diagnoses.PlotAccessDenied",
                    "Access to this plot is not allowed.")));
    }

    public Task<Result<IReadOnlyCollection<int>>> GetAccessiblePlotIdsAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyCollection<int> ids = allowedPlotId > 0
            ? new[] { allowedPlotId }
            : Array.Empty<int>();

        return Task.FromResult(
            Result<IReadOnlyCollection<int>>.Success(ids));
    }
}

internal sealed class FailingImageAnalysis : IImageAnalysis
{
    public Task<ImageAnalysisResult> AnalyzeAsync(
        ImageAnalysisInput input,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        throw new InvalidOperationException(
            "Internal test provider failure.");
    }
}

// ===== LOCAL DEMONSTRATION DATA =====

internal static class DiagnosisDemoSeed
{
    public static async Task RunAsync()
    {
        var connection = Environment.GetEnvironmentVariable(
            "SKYCROP_TEST_CONNECTION");

        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException(
                "Configure SKYCROP_TEST_CONNECTION.");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(connection)
            .Options;

        await using var context = new AppDbContext(options);

        if (context.Database.GetDbConnection().Database
            != "skycrop_diagnoses_checks")
        {
            throw new InvalidOperationException(
                "Demo data requires skycrop_diagnoses_checks.");
        }

        if ((await context.Database.GetPendingMigrationsAsync()).Any())
            throw new InvalidOperationException(
                "Apply pending migrations before creating demo data.");

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var plot = new AgriculturalPlot(new RegisterPlotCommand(
            "Diagnoses local demo",
            "Demo operator",
            "Potato",
            "Loam",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
            1m,
            "Demo north sector",
            -12.0464,
            -77.0428,
            null));

        context.Add(plot);
        await context.SaveChangesAsync();

        var drone = new Drone(new CreateDroneCommand(
            "Diagnosis demo drone",
            "Demo model",
            $"demo-{Guid.NewGuid():N}",
            "Demo camera",
            30,
            100));

        context.Add(drone);
        await context.SaveChangesAsync();

        drone.Update(new UpdateDroneCommand(
            drone.Id, Status: "AVAILABLE"));

        var route = new FlightRoute(new CreateFlightRouteCommand(
            "Diagnosis demo route",
            plot.Id,
            30,
            4,
            new[]
            {
                new RouteWaypoint(0, -12.0464, -77.0428),
                new RouteWaypoint(1, -12.0465, -77.0429)
            }));

        context.Add(route);
        await context.SaveChangesAsync();

        var flight = new Flight(drone, route, plot.Id);
        context.Add(flight);
        await context.SaveChangesAsync();

        // Demo fixture only. This does not execute or complete a real flight.
        context.Entry(flight)
            .Property(item => item.Status)
            .CurrentValue = EFlightStatus.COMPLETED;

        context.Entry(flight)
            .Property(item => item.ActiveDroneId)
            .CurrentValue = null;

        var image = new AerialImage(
            flight.Id,
            "https://example.com/diagnoses-demo.jpg",
            DateTimeOffset.UtcNow.AddMinutes(-10));

        context.Add(image);
        await context.SaveChangesAsync();

        await transaction.CommitAsync();

        Console.WriteLine("Demo fixtures committed.");
        Console.WriteLine($"Diagnoses__DemoPlotId = {plot.Id}");
        Console.WriteLine("Use this POST body:");

        Console.WriteLine(JsonSerializer.Serialize(new
        {
            plotId = plot.Id,
            flightId = flight.Id,
            imageIds = new[] { image.Id },
            scenario = "WATER_STRESS"
        }, new JsonSerializerOptions { WriteIndented = true }));
    }
}
