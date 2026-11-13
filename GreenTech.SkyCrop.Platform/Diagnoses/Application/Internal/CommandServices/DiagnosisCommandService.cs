using GreenTech.SkyCrop.Platform.Diagnoses.Application.CommandServices;
using GreenTech.SkyCrop.Platform.Diagnoses.Application.IntegrationServices;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Commands;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Repositories;
using GreenTech.SkyCrop.Platform.Shared.Application.Model;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Errors;
using GreenTech.SkyCrop.Platform.Shared.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace GreenTech.SkyCrop.Platform.Diagnoses.Application.Internal.CommandServices;

/// <summary>
/// Generates a simulated diagnosis after checking access and monitoring data.
/// Persists processing state before analysis and the terminal state afterwards.
/// </summary>
public sealed class DiagnosisCommandService(
    IDiagnosisRepository repository,
    IUnitOfWork unitOfWork,
    IPlotAccessService access,
    IMonitoringDataSource monitoring,
    IImageAnalysis analysis,
    TimeProvider clock,
    ILogger<DiagnosisCommandService> logger)
    : IDiagnosisCommandService
{
    public async Task<Result<Diagnosis>> Handle(
        GenerateDiagnosisCommand command,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (command is null || command.ImageIds is null)
            return Invalid("The request and image identifiers are required.");

        // Copy caller-owned collections before awaiting external operations.
        var imageIds = command.ImageIds.ToArray();

        if (command.PlotId <= 0 || command.FlightId <= 0 ||
            imageIds.Length is < 1 or > 100 ||
            imageIds.Any(id => id <= 0) ||
            imageIds.Distinct().Count() != imageIds.Length)
        {
            return Invalid(
                "Provide positive plot and flight identifiers and 1 to 100 unique image identifiers.");
        }

        var scenario = command.Scenario?.Trim().ToUpperInvariant();

        if (scenario is not ("HEALTHY" or "WATER_STRESS" or "PEST"))
            return Invalid("Unknown simulation scenario.");

        var permission = await access.CheckAccessAsync(
            command.PlotId, cancellationToken);

        if (permission.IsFailure)
            return Result<Diagnosis>.Failure(permission.DomainError);

        var monitoringResult = await monitoring.GetAsync(
            command.PlotId,
            command.FlightId,
            imageIds,
            cancellationToken);

        if (monitoringResult.IsFailure)
            return Result<Diagnosis>.Failure(monitoringResult.DomainError);

        var data = monitoringResult.Value!;
        Diagnosis diagnosis;

        try
        {
            diagnosis = new Diagnosis(
                data.PlotId,
                data.FlightId,
                data.ImageIds,
                scenario,
                data.MonitoredAt,
                clock.GetUtcNow());
        }
        catch (ArgumentException exception)
        {
            return Invalid(exception.Message);
        }

        diagnosis.StartProcessing();

        await repository.AddAsync(diagnosis, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);

        try
        {
            var result = await analysis.AnalyzeAsync(
                new ImageAnalysisInput(
                    diagnosis.Scenario,
                    data.Location,
                    data.Latitude,
                    data.Longitude),
                cancellationToken);

            diagnosis.CompleteSimulated(
                result.Observations,
                result.AverageNdvi,
                result.Anomalies,
                clock.GetUtcNow());
        }
        catch (OperationCanceledException)
        {
            diagnosis.Fail("Analysis was cancelled.");

            using var timeout = new CancellationTokenSource(
                TimeSpan.FromSeconds(10));

            await unitOfWork.CompleteAsync(timeout.Token);
            throw;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            logger.LogError(
                exception,
                "Analysis failed for diagnosis {DiagnosisId}",
                diagnosis.Id);

            // Do not expose internal exception details in persisted results.
            diagnosis.Fail("Analysis could not be completed.");
        }

        // Finish persistence even if the HTTP caller disconnects after analysis.
        using var completionTimeout = new CancellationTokenSource(
            TimeSpan.FromSeconds(10));

        await unitOfWork.CompleteAsync(completionTimeout.Token);

        return Result<Diagnosis>.Success(diagnosis);
    }

    private static Result<Diagnosis> Invalid(string message) =>
        Result<Diagnosis>.Failure(DiagnosisErrors.InvalidRequest(message));
}