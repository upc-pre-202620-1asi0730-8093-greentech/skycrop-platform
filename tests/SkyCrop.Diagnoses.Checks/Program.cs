using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Entities;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.ValueObjects;
using GreenTech.SkyCrop.Platform.Diagnoses.Application.IntegrationServices;
using GreenTech.SkyCrop.Platform.Diagnoses.Infrastructure.ImageAnalysis;

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

Console.WriteLine();
Console.WriteLine($"Results: {passed} passed, {failed} failed.");

Environment.ExitCode = failed == 0 ? 0 : 1;