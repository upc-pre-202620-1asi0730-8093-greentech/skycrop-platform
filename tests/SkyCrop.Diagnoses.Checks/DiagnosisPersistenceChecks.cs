using GreenTech.SkyCrop.Platform.Diagnoses.Application.IntegrationServices;
using GreenTech.SkyCrop.Platform.Diagnoses.Application.Internal.CommandServices;
using GreenTech.SkyCrop.Platform.Diagnoses.Application.Internal.QueryServices;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Commands;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Queries;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.ValueObjects;
using GreenTech.SkyCrop.Platform.Diagnoses.Infrastructure.ImageAnalysis;
using GreenTech.SkyCrop.Platform.Diagnoses.Infrastructure.Integration;
using GreenTech.SkyCrop.Platform.Diagnoses.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using GreenTech.SkyCrop.Platform.Drones.Domain.Model;
using GreenTech.SkyCrop.Platform.Drones.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Drones.Domain.Model.Commands;
using GreenTech.SkyCrop.Platform.Drones.Domain.Model.Entities;
using GreenTech.SkyCrop.Platform.Plots.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Plots.Domain.Model.Commands;
using GreenTech.SkyCrop.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using GreenTech.SkyCrop.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using GreenTech.SkyCrop.Platform.Diagnoses.Application.Acl;
using Cortex.Mediator;
using Microsoft.Extensions.DependencyInjection;
using GreenTech.SkyCrop.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

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