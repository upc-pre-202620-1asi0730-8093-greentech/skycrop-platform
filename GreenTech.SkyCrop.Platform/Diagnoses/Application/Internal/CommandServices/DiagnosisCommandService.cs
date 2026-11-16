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
/// Generates a standalone simulated diagnosis without external context dependencies.
/// Persists processing state before analysis and the terminal state afterwards.
/// </summary>
public sealed class DiagnosisCommandService(
    IDiagnosisRepository repository,
    IUnitOfWork unitOfWork,
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

        if (command is null)
            return Invalid("The request is required.");

        var scenario = command.Scenario?.Trim().ToUpperInvariant();

        if (scenario is not ("HEALTHY" or "WATER_STRESS" or "PEST"))
            return Invalid("Unknown simulation scenario.");

        var diagnosis = new Diagnosis(scenario!, clock.GetUtcNow());

        diagnosis.StartProcessing();

        await repository.AddAsync(diagnosis, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);

        try
        {
            var result = await analysis.AnalyzeAsync(
                new ImageAnalysisInput(
                    diagnosis.Scenario,
                    "Simulated analysis",
                    null,
                    null),
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