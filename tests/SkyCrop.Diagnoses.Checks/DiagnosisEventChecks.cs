using System.Collections.Concurrent;
using Cortex.Mediator.DependencyInjection;
using Cortex.Mediator.Notifications;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Entities;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Events;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.ValueObjects;
using GreenTech.SkyCrop.Platform.Diagnoses.Interfaces.Events;
using GreenTech.SkyCrop.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Microsoft.Extensions.DependencyInjection;

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