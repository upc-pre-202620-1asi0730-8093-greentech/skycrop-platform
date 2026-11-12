using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Entities;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.ValueObjects;
using GreenTech.SkyCrop.Platform.Diagnoses.Application.IntegrationServices;
using GreenTech.SkyCrop.Platform.Diagnoses.Infrastructure.ImageAnalysis;
using GreenTech.SkyCrop.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Repositories;
using GreenTech.SkyCrop.Platform.Diagnoses.Infrastructure.Configuration;
using GreenTech.SkyCrop.Platform.Diagnoses.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

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

var analyzer = new SimulatedImageAnalysis();

ImageAnalysisResult Analyze(string scenario) =>
    analyzer.AnalyzeAsync(
        new ImageAnalysisInput(
            scenario, "North sector", -12.0464, -77.0428))
    .GetAwaiter().GetResult();

Test("Healthy analysis returns NDVI 0.8 without anomalies", () =>
{
    var result = Analyze("HEALTHY");

    Check(result.AverageNdvi.Value == 0.8 &&
          result.Anomalies.Count == 0,
        "Unexpected healthy result.");
});

Test("Water stress analysis returns the frontend scenario values", () =>
{
    var result = Analyze("WATER_STRESS");
    var anomaly = result.Anomalies.Single();

    Check(result.AverageNdvi.Value == 0.4 &&
          anomaly.Type == "WATER_STRESS" &&
          anomaly.Severity == "MEDIUM",
        "Unexpected water stress result.");

    Check(anomaly.Latitude == -12.0464 &&
          anomaly.Longitude == -77.0428 &&
          anomaly.Location == "North sector",
        "Analysis did not preserve location data.");
});

Test("Pest analysis returns NDVI 0.55 and high severity", () =>
{
    var result = Analyze("PEST");
    var anomaly = result.Anomalies.Single();

    Check(result.AverageNdvi.Value == 0.55 &&
          anomaly.Type == "PEST" &&
          anomaly.Severity == "HIGH",
        "Unexpected pest result.");
});

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

Test("Each simulated result can complete its matching diagnosis", () =>
{
    foreach (var scenario in new[] { "HEALTHY", "WATER_STRESS", "PEST" })
    {
        var diagnosis = CreateDiagnosis(scenario);
        var result = Analyze(scenario);

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
            $"Analysis could not complete the {scenario} diagnosis.");
    }
});

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

Console.WriteLine();
Console.WriteLine($"Results: {passed} passed, {failed} failed.");

Environment.ExitCode = failed == 0 ? 0 : 1;