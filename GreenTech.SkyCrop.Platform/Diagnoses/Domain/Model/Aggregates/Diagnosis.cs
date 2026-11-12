using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Entities;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.ValueObjects;
using GreenTech.SkyCrop.Platform.Shared.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Shared.Domain.Model.Entities;

namespace GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Aggregates;

/// <summary>
/// Owns diagnosis results and controls the analysis lifecycle.
/// References other bounded contexts through identifiers.
/// </summary>
public sealed class Diagnosis : AggregateRoot, IAuditableEntity
{
    private readonly List<Anomaly> _anomalies = [];
    private readonly List<int> _imageIds = [];

    // Required by Entity Framework Core.
    private Diagnosis() { }

    public Diagnosis(
        int plotId,
        int flightId,
        IEnumerable<int> imageIds,
        string scenario,
        DateTimeOffset monitoredAt,
        DateTimeOffset requestedAt)
    {
        if (plotId <= 0)
            throw new ArgumentException(
                "A positive plot identifier is required.", nameof(plotId));

        if (flightId <= 0)
            throw new ArgumentException(
                "A positive flight identifier is required.", nameof(flightId));

        ArgumentNullException.ThrowIfNull(imageIds);

        var ids = imageIds.ToArray();

        if (ids.Length is < 1 or > 100 ||
            ids.Any(id => id <= 0) ||
            ids.Distinct().Count() != ids.Length)
        {
            throw new ArgumentException(
                "Provide between 1 and 100 unique, positive image identifiers.",
                nameof(imageIds));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(scenario);
        var normalizedScenario = scenario.Trim().ToUpperInvariant();

        if (normalizedScenario is not ("HEALTHY" or "WATER_STRESS" or "PEST"))
            throw new ArgumentException(
                "Unknown simulation scenario.", nameof(scenario));

        if (monitoredAt == default ||
            requestedAt == default ||
            monitoredAt > requestedAt)
        {
            throw new ArgumentException(
                "Monitoring date must not be later than the request date.");
        }

        PlotId = plotId;
        FlightId = flightId;
        Scenario = normalizedScenario;
        MonitoredAt = monitoredAt.ToUniversalTime();
        RequestedAt = requestedAt.ToUniversalTime();

        _imageIds.AddRange(ids);
    }

    public int Id { get; private set; }
    public int PlotId { get; private set; }
    public int FlightId { get; private set; }

    public EDiagnosisStatus Status { get; private set; }
        = EDiagnosisStatus.PENDING;

    public string Scenario { get; private set; } = null!;
    public string Source { get; private set; } = "SIMULATED";

    public DateTimeOffset MonitoredAt { get; private set; }
    public DateTimeOffset RequestedAt { get; private set; }
    public DateTimeOffset? GeneratedAt { get; private set; }

    public string Observations { get; private set; } = string.Empty;
    public Ndvi? AverageNdvi { get; private set; }
    public string? FailureReason { get; private set; }

    public IReadOnlyCollection<int> ImageIds => _imageIds.AsReadOnly();

    public IReadOnlyCollection<Anomaly> Anomalies =>
        _anomalies.AsReadOnly();

    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public void StartProcessing()
    {
        if (Status != EDiagnosisStatus.PENDING)
            throw new InvalidOperationException(
                "Only pending diagnoses can start processing.");

        Status = EDiagnosisStatus.PROCESSING;
    }

    public void CompleteSimulated(
        string observations,
        Ndvi averageNdvi,
        IEnumerable<Anomaly> anomalies,
        DateTimeOffset generatedAt)
    {
        if (Status != EDiagnosisStatus.PROCESSING)
            throw new InvalidOperationException(
                "Only processing diagnoses can be completed.");

        ArgumentException.ThrowIfNullOrWhiteSpace(observations);
        ArgumentNullException.ThrowIfNull(averageNdvi);
        ArgumentNullException.ThrowIfNull(anomalies);

        var text = observations.Trim();

        if (text.Length > 2000)
            throw new ArgumentException(
                "Observations must not exceed 2000 characters.",
                nameof(observations));

        if (generatedAt == default || generatedAt < RequestedAt)
            throw new ArgumentException(
                "Generation date must not precede the request date.",
                nameof(generatedAt));

        var results = anomalies.ToArray();

        if (results.Any(anomaly => anomaly is null))
            throw new ArgumentException(
                "Anomalies must not contain null entries.",
                nameof(anomalies));

        if (results.Any(anomaly =>
                anomaly.Id != 0 || anomaly.DiagnosisId != 0))
        {
            throw new ArgumentException(
                "Use new anomalies for this diagnosis.",
                nameof(anomalies));
        }

        if (results.Distinct().Count() != results.Length)
            throw new ArgumentException(
                "The same anomaly instance cannot be added twice.",
                nameof(anomalies));

        if (Scenario == "HEALTHY" && results.Length != 0)
            throw new ArgumentException(
                "A healthy simulation must not contain anomalies.",
                nameof(anomalies));

        if (Scenario != "HEALTHY" &&
            (results.Length == 0 ||
             results.Any(anomaly => anomaly.Type != Scenario)))
        {
            throw new ArgumentException(
                "Anomalies must match the selected simulation scenario.",
                nameof(anomalies));
        }

        Observations = text;
        AverageNdvi = averageNdvi;
        GeneratedAt = generatedAt.ToUniversalTime();
        FailureReason = null;

        _anomalies.AddRange(results);
        Status = EDiagnosisStatus.COMPLETED;
    }

    public void Fail(string reason)
    {
        if (Status is not (
            EDiagnosisStatus.PENDING or EDiagnosisStatus.PROCESSING))
        {
            throw new InvalidOperationException(
                "Completed or failed diagnoses cannot change status.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        var text = reason.Trim();

        if (text.Length > 1000)
            throw new ArgumentException(
                "Failure reason must not exceed 1000 characters.",
                nameof(reason));

        FailureReason = text;
        Status = EDiagnosisStatus.FAILED;
    }
}