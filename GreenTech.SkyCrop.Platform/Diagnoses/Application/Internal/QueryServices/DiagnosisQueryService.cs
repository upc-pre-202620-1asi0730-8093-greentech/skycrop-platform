using GreenTech.SkyCrop.Platform.Diagnoses.Application.IntegrationServices;
using GreenTech.SkyCrop.Platform.Diagnoses.Application.QueryServices;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Queries;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Repositories;
using GreenTech.SkyCrop.Platform.Shared.Application.Model;
using GreenTech.SkyCrop.Platform.Shared.Domain.Model.Errors;

namespace GreenTech.SkyCrop.Platform.Diagnoses.Application.Internal.QueryServices;

/// <summary>
/// Retrieves diagnoses while enforcing plot access restrictions.
/// </summary>
public sealed class DiagnosisQueryService(
    IDiagnosisRepository repository,
    IPlotAccessService access)
    : IDiagnosisQueryService
{
    public async Task<Result<Diagnosis>> Handle(
        GetDiagnosisByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(query);

        if (query.DiagnosisId <= 0)
            return Result<Diagnosis>.Failure(Error.Validation(
                "Diagnoses.InvalidDiagnosisId",
                "A positive diagnosis identifier is required."));

        var permissions = await access.GetAccessiblePlotIdsAsync(
            cancellationToken);

        if (permissions.IsFailure)
            return Result<Diagnosis>.Failure(permissions.DomainError);

        var allowedPlots = permissions.Value!;

        if (allowedPlots.Count == 0)
            return NotFound();

        var diagnosis = await repository.FindByIdAsNoTrackingAsync(
            query.DiagnosisId, cancellationToken);

        // Do not reveal whether a diagnosis exists in an inaccessible plot.
        if (diagnosis is null || !allowedPlots.Contains(diagnosis.PlotId))
            return NotFound();

        return Result<Diagnosis>.Success(diagnosis);
    }

    public async Task<Result<IEnumerable<Diagnosis>>> Handle(
        GetDiagnosesByPlotIdQuery query,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(query);

        if (query.PlotId <= 0)
            return Result<IEnumerable<Diagnosis>>.Failure(Error.Validation(
                "Diagnoses.InvalidPlotId",
                "A positive plot identifier is required."));

        var permission = await access.CheckAccessAsync(
            query.PlotId, cancellationToken);

        if (permission.IsFailure)
            return Result<IEnumerable<Diagnosis>>.Failure(
                permission.DomainError);

        var diagnoses = await repository.FindByPlotIdAsync(
            query.PlotId, cancellationToken);

        return Result<IEnumerable<Diagnosis>>.Success(diagnoses);
    }

    public async Task<Result<IEnumerable<Diagnosis>>> Handle(
        GetAllDiagnosesQuery query,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(query);

        var permissions = await access.GetAccessiblePlotIdsAsync(
            cancellationToken);

        if (permissions.IsFailure)
            return Result<IEnumerable<Diagnosis>>.Failure(
                permissions.DomainError);

        var diagnoses = await repository.FindByPlotIdsAsync(
            permissions.Value!, cancellationToken);

        return Result<IEnumerable<Diagnosis>>.Success(diagnoses);
    }

    private static Result<Diagnosis> NotFound() =>
        Result<Diagnosis>.Failure(Error.NotFound(
            "Diagnoses.DiagnosisNotFound",
            "The diagnosis was not found."));
}