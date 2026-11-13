using GreenTech.SkyCrop.Platform.Diagnoses.Application.QueryServices;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Queries;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.ValueObjects;
using GreenTech.SkyCrop.Platform.Diagnoses.Interfaces.Acl;
using GreenTech.SkyCrop.Platform.Shared.Application.Model;

namespace GreenTech.SkyCrop.Platform.Diagnoses.Application.Acl;

/// <summary>
/// Exposes completed diagnosis summaries without sharing domain entities.
/// </summary>
public sealed class DiagnosesContextFacade(
    IDiagnosisQueryService queries) : IDiagnosesContextFacade
{
    public async Task<Result<DiagnosisSummary?>>
        GetLatestCompletedByPlotIdAsync(
            int plotId,
            CancellationToken cancellationToken = default)
    {
        var result = await queries.Handle(
            new GetDiagnosesByPlotIdQuery(plotId),
            cancellationToken);

        if (result.IsFailure)
            return Result<DiagnosisSummary?>.Failure(result.DomainError);

        var latest = result.Value!
            .Where(diagnosis =>
                diagnosis.Status == EDiagnosisStatus.COMPLETED)
            .OrderByDescending(diagnosis => diagnosis.MonitoredAt)
            .ThenByDescending(diagnosis => diagnosis.Id)
            .FirstOrDefault();

        if (latest is null)
            return Result<DiagnosisSummary?>.Success(null);

        var summary = new DiagnosisSummary(
            latest.Id,
            latest.PlotId,
            latest.MonitoredAt,
            latest.GeneratedAt!.Value,
            latest.Scenario,
            latest.Source,
            latest.AverageNdvi!.Value,
            latest.Observations,
            latest.Anomalies.Count);

        return Result<DiagnosisSummary?>.Success(summary);
    }
}