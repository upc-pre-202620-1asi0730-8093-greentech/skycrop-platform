using GreenTech.SkyCrop.Platform.Diagnoses.Application.IntegrationServices;
using GreenTech.SkyCrop.Platform.Diagnoses.Application.QueryServices;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Errors;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Queries;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Repositories;
using GreenTech.SkyCrop.Platform.Shared.Application.Model;

namespace GreenTech.SkyCrop.Platform.Diagnoses.Application.Internal.QueryServices;

public sealed class DiagnosisQueryService(
    IDiagnosisRepository repository,
    IPlotAccessService access) : IDiagnosisQueryService
{
    public async Task<Result<Diagnosis>> Handle(
        GetDiagnosisByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(query);

        if (query.DiagnosisId <= 0)
            return Result<Diagnosis>.Failure(DiagnosisErrors.InvalidDiagnosisId);

        //TODO: Implement access check for plots
        //
        // var permissions = await access.GetAccessiblePlotIdsAsync(cancellationToken);
        //
        // if (permissions.IsFailure)
        //     return Result<Diagnosis>.Failure(permissions.DomainError);
        //
        // var allowedPlots = permissions.Value!;
        //
        // if (allowedPlots.Count == 0)
        //     return Result<Diagnosis>.Failure(DiagnosisErrors.NotFound);

        var diagnosis = await repository.FindByIdAsNoTrackingAsync(
            query.DiagnosisId, cancellationToken);

        // Inaccessible and nonexistent diagnoses return the same error.
        // if (diagnosis is null || !allowedPlots.Contains(diagnosis.PlotId))
        //     return Result<Diagnosis>.Failure(DiagnosisErrors.NotFound);

        if (diagnosis is null)
            return Result<Diagnosis>.Failure(DiagnosisErrors.NotFound);

        return Result<Diagnosis>.Success(diagnosis);
    }

    public async Task<Result<IEnumerable<Diagnosis>>> Handle(
        GetDiagnosesByPlotIdQuery query,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(query);

        if (query.PlotId <= 0)
            return Result<IEnumerable<Diagnosis>>.Failure(DiagnosisErrors.InvalidPlotId);

        //TODO: Implement access check for plots
        //
        // var permission = await access.CheckAccessAsync(query.PlotId, cancellationToken);
        //
        // if (permission.IsFailure)
        //     return Result<IEnumerable<Diagnosis>>.Failure(permission.DomainError);

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

        // TODO: Implement access check for plots
        //
        // var permissions = await access.GetAccessiblePlotIdsAsync(cancellationToken);
        //
        // if (permissions.IsFailure)
        //     return Result<IEnumerable<Diagnosis>>.Failure(permissions.DomainError);

        // var diagnoses = await repository.FindByPlotIdsAsync(
        //     permissions.Value!, cancellationToken);
        
        var diagnoses = await repository.ListAsNoTrackingAsync(cancellationToken);

        return Result<IEnumerable<Diagnosis>>.Success(diagnoses);
    }
}